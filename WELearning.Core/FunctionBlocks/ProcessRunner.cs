using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessRunner : IProcessRunner
{
    private readonly IBlockRunner _blockRunner;
    public ProcessRunner(IBlockRunner blockRunner)
    {
        _blockRunner = blockRunner;
    }

    public virtual async Task Run(RunProcessRequest request, ProcessExecutionContext processContext, ProcessExecutionControl processControl)
    {
        processControl.Status = EProcessExecutionStatus.Running;
        try
        {
            var startingBlockTriggers = request.Triggers
                ?? request.Process.DefaultBlockIds.Select(bId => new BlockTrigger(blockId: bId, triggerEvent: null));
            RunBlocks(request.Process, blockTriggers: startingBlockTriggers, processContext, processControl);
            await Task.WhenAll(processControl.ProcessTasks.Select(t => t.ExecutionTask));
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
        ProcessExecutionControl processControl)
    {
        foreach (var trigger in blockTriggers)
        {
            var block = process.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
            if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
            WaitForCompletion(processControl, block.Id).ContinueWith(async (task) =>
            {
                var blockControl = GetBlockExecutionControl(processControl, block);
                await WaitAndPrepareInputs(trigger.TriggerEvent, block, process, processContext, processControl, blockControl);
                var runRequest = new RunBlockRequest(block, trigger.TriggerEvent);
                await RunBlock(runRequest, process, processContext, processControl, blockControl);
            });
        }
    }

    public virtual async Task RunBlock(
        RunBlockRequest request,
        FunctionBlockProcess process,
        ProcessExecutionContext processContext,
        ProcessExecutionControl processControl,
        BlockExecutionControl blockControl)
    {
        var block = request.Block;
        var startTime = DateTime.UtcNow;
        var executionTask = _blockRunner.Run(request, blockControl);
        processControl.ProcessTasks.Add(new(blockId: block.Id, startTime, executionTask));
        var blockResult = await executionTask;
        var nextBlockTriggers = FindNextBlocks(process, block.Id, outputEvents: blockResult.OutputEvents);
        RunBlocks(process, nextBlockTriggers, processContext, processControl);
    }

    protected virtual async Task WaitAndPrepareInputs(
        string triggerEvent, FunctionBlock block, FunctionBlockProcess process,
        ProcessExecutionContext processContext,
        ProcessExecutionControl processControl,
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
                        blockControl.InputSnapshot[connection.VariableName] = binding.Value;
                        break;
                    }

                case EDataSource.Internal:
                    {
                        if (connection.SourceBlockId != null && connection.SourceVariableName != null)
                        {
                            await WaitForCompletion(processControl, connection.SourceBlockId);
                            var sourceBlock = process.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId);
                            var sourceBlockControl = GetBlockExecutionControl(processControl, sourceBlock);
                            var value = sourceBlockControl.OutputSnapshot[connection.SourceVariableName];
                            blockControl.InputSnapshot[connection.VariableName] = value;
                        }
                        else
                            blockControl.InputSnapshot[connection.VariableName] = connection.ConstantValue;
                        break;
                    }
            }
        }
    }

    protected virtual async Task<BlockExecutionTaskInfo> WaitForCompletion(ProcessExecutionControl control, string blockId)
    {
        var blockRunningTask = control.ProcessTasks
            .Where(t => t.BlockId == blockId)
            .OrderByDescending(t => t.StartTime)
            .FirstOrDefault();
        if (blockRunningTask != null && blockRunningTask.CompletedTime == null)
            await blockRunningTask.ExecutionTask;
        return blockRunningTask;
    }

    protected virtual BlockExecutionControl GetBlockExecutionControl(ProcessExecutionControl control, FunctionBlock block)
        => control.BlockExecutionControlMap.GetOrAdd(block.Id, (key) => new BlockExecutionControl(block.ExecutionControlChart.InitialState));
}