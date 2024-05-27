using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessRunner<TFramework> : IProcessRunner<TFramework>
{
    private readonly IBlockRunner<TFramework> _blockRunner;
    private readonly IBlockFrameworkFactory<TFramework> _blockFrameworkFactory;
    public ProcessRunner(IBlockRunner<TFramework> blockRunner, IBlockFrameworkFactory<TFramework> blockFrameworkFactory)
    {
        _blockRunner = blockRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
    }

    public virtual async Task Run(RunProcessRequest request, ProcessExecutionContext processContext, ProcessExecutionControl<TFramework> processControl)
    {
        processControl.Status = EProcessExecutionStatus.Running;
        try
        {
            var startingBlockTriggers = request.Triggers
                ?? request.Process.DefaultBlockIds.Select(bId => new BlockTrigger(blockId: bId, triggerEvent: null));
            _ = RunAsync(() =>
            {
                RunBlocks(request.Process, blockTriggers: startingBlockTriggers, processContext, processControl);
                return Task.CompletedTask;
            }, processControl);
            await WaitForCompletion(processControl);
            processControl.Status = EProcessExecutionStatus.Completed;
        }
        catch (Exception ex)
        {
            processControl.Exception = ex;
            processControl.Status = EProcessExecutionStatus.Failed;
            throw;
        }
    }

    public virtual IEnumerable<BlockTrigger> FindNextBlocks(
        FunctionBlockProcess process,
        string sourceBlockId,
        IEnumerable<string> outputEvents)
    {
        var eventConnections = process.EventConnections;
        var nextBlockTriggers = eventConnections
            .Where(c => c.Source == EEventSource.Internal && c.SourceBlockId == sourceBlockId && outputEvents.Contains(c.SourceEventName))
            .Select(c => new BlockTrigger(blockId: c.BlockId, triggerEvent: c.EventName));
        return nextBlockTriggers;
    }

    public virtual void RunBlocks(
        FunctionBlockProcess process,
        IEnumerable<BlockTrigger> blockTriggers,
        ProcessExecutionContext processContext,
        ProcessExecutionControl<TFramework> processControl)
    {
        foreach (var trigger in blockTriggers)
        {
            var block = process.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
            if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
            _ = RunAsync(async () =>
            {
                await WaitForCompletion(processControl, block.Id);
                var blockControl = GetBlockExecutionControl(block, process, processContext, processControl);
                var triggerEvent = trigger.TriggerEvent ?? block.DefaultTriggerEvent;
                await WaitAndPrepareInputs(triggerEvent, block, process, processContext, processControl, blockControl);
                var runRequest = new RunBlockRequest(block, triggerEvent);
                var blockFramework = _blockFrameworkFactory.Create(blockControl);
                await RunBlock(runRequest, process, processContext, processControl, blockControl, blockFramework);
            }, processControl);
        }
    }

    public virtual async Task RunBlock(
        RunBlockRequest request,
        FunctionBlockProcess process,
        ProcessExecutionContext processContext,
        ProcessExecutionControl<TFramework> processControl,
        BlockExecutionControl blockControl,
        TFramework blockFramework)
    {
        var block = request.Block;
        var startTime = DateTime.UtcNow;
        var executionTask = _blockRunner.Run(request, blockControl, blockFramework);
        processControl.ExecutionTasks.Add(new(blockId: block.Id, startTime, executionTask));
        var blockResult = await executionTask;
        var nextBlockTriggers = FindNextBlocks(process, block.Id, outputEvents: blockResult.OutputEvents);
        RunBlocks(process, nextBlockTriggers, processContext, processControl);
    }

    protected virtual async Task WaitAndPrepareInputs(
        string triggerEvent, FunctionBlock block, FunctionBlockProcess process,
        ProcessExecutionContext processContext,
        ProcessExecutionControl<TFramework> processControl,
        BlockExecutionControl blockControl)
    {
        var inputEvent = block.InputEvents.FirstOrDefault(ev => ev.Name == triggerEvent);
        if (inputEvent == null) throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var eventDataConnections = process.DataConnections
            .Where(c => c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName));
        foreach (var connection in eventDataConnections)
        {
            switch (connection.Source)
            {
                case EDataSource.External:
                    {
                        var binding = processContext.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.VariableName == connection.VariableName);
                        if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                        blockControl.GetInput(connection.VariableName).Value = binding.Value;
                        break;
                    }

                case EDataSource.Internal:
                    {
                        if (connection.SourceBlockId != null && connection.SourceVariableName != null)
                        {
                            await WaitForCompletion(processControl, connection.SourceBlockId);
                            var sourceBlock = process.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId);
                            var sourceBlockControl = GetBlockExecutionControl(sourceBlock, process, processContext, processControl);
                            var outputValue = sourceBlockControl.GetOutput(connection.SourceVariableName);
                            outputValue.ValueSet.Wait();
                            blockControl.GetInput(connection.VariableName).Value = outputValue.Value;
                        }
                        else
                            blockControl.GetInput(connection.VariableName).Value = connection.ConstantValue;
                        break;
                    }
            }
        }
    }

    protected virtual Task<BlockExecutionTaskInfo> WaitForCompletion(ProcessExecutionControl<TFramework> processControl, string blockId)
        => processControl.WaitForCompletion(blockId);

    protected virtual Task WaitForCompletion(ProcessExecutionControl<TFramework> processControl)
    {
        processControl.WaitForCompletion();
        return Task.CompletedTask;
    }

    protected virtual BlockExecutionControl GetBlockExecutionControl(FunctionBlock block, FunctionBlockProcess process, ProcessExecutionContext processContext, ProcessExecutionControl<TFramework> processControl)
        => processControl.BlockExecutionControlMap.GetOrAdd(block.Id, (key) =>
        {
            var blockControl = new BlockExecutionControl(blockId: block.Id, initialState: block.ExecutionControlChart.InitialState);
            var outputVariables = block.Outputs.Select(o => o.Name).ToArray();
            var blockExternalOutputs = process.DataConnections
                .Where(c => c.BlockId == block.Id && c.Source == EDataSource.External && outputVariables.Contains(c.VariableName));

            foreach (var connection in blockExternalOutputs)
            {
                var binding = processContext.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.VariableName == connection.VariableName);
                if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                blockControl.GetOutput(connection.VariableName).Value = binding.Value;
            }

            return blockControl;
        });

    private static async Task RunAsync(Func<Task> func, ProcessExecutionControl<TFramework> processControl)
    {
        processControl.StartProcess();
        try
        {
            await Task.Yield();
            await func();
        }
        finally { processControl.CompleteProcess(); }
    }
}