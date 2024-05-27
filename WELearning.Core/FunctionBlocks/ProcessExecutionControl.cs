using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessExecutionControl : IProcessExecutionControl
{
    private readonly ProcessExecutionContext _context;
    private readonly ManualResetEventSlim _processIdleWait;
    private ConcurrentDictionary<string, BlockExecutionControl> _blockExecutionControlMap;
    private readonly ConcurrentBag<BlockExecutionTaskInfo> _executionTasks;

    public ProcessExecutionControl(FunctionBlockProcess process, ProcessExecutionContext context)
    {
        _context = context;
        _blockRunningProcessCount = 0;
        _blockExecutionControlMap = new();
        _processIdleWait = new ManualResetEventSlim();
        _executionTasks = new();
        Process = process;
    }

    public FunctionBlockProcess Process { get; }
    private int _blockRunningProcessCount;
    public virtual int BlockRunningProcessCount => _blockRunningProcessCount;
    public virtual IEnumerable<BlockExecutionTaskInfo> ExecutionTasks => _executionTasks;
    public virtual Exception Exception { get; protected set; }
    public virtual EProcessExecutionStatus Status { get; protected set; }

    protected virtual void StartProcess()
    {
        lock (_processIdleWait)
        {
            _blockRunningProcessCount++;
            _processIdleWait.Reset();
        }
    }

    protected virtual void CompleteProcess()
    {
        lock (_processIdleWait)
        {
            if (_blockRunningProcessCount == 0) return;
            _blockRunningProcessCount--;
            if (_blockRunningProcessCount == 0)
                _processIdleWait.Set();
        }
    }

    public virtual void WaitForCompletion() => _processIdleWait.Wait();

    public virtual async Task<BlockExecutionTaskInfo> WaitForCompletion(string blockId)
    {
        var blockRunningTask = _executionTasks
            .Where(t => t.BlockId == blockId)
            .OrderByDescending(t => t.StartTime)
            .FirstOrDefault();
        if (blockRunningTask != null && blockRunningTask.CompletedTime == null)
            await blockRunningTask.ExecutionTask;
        return blockRunningTask;
    }

    public virtual IBlockExecutionControl GetBlockControl(string blockId)
        => _blockExecutionControlMap[blockId];

    public virtual IBlockExecutionControl GetOrInitBlockControl(FunctionBlockInstance block)
        => _blockExecutionControlMap.GetOrAdd(block.Id, (key) =>
        {
            var blockControl = new BlockExecutionControl(block);
            var outputVariables = block.Definition.Outputs.Select(o => o.Name).ToArray();
            var blockExternalOutputs = Process.DataConnections
                .Where(c => c.BlockId == block.Id && c.Source == EDataSource.External && outputVariables.Contains(c.VariableName));

            foreach (var connection in blockExternalOutputs)
            {
                var binding = _context.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.VariableName == connection.VariableName);
                if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                blockControl.GetOutput(connection.VariableName).Value = binding.Value;
            }

            return blockControl;
        });

    protected async Task RunTaskAsync(Func<Task> func)
    {
        StartProcess();
        try
        {
            await Task.Yield();
            await func();
        }
        finally { CompleteProcess(); }
    }

    public virtual async Task Run(RunProcessRequest request,
        Func<RunBlockRequest, IBlockExecutionControl, Task<BlockExecutionResult>> RunBlock)
    {
        Status = EProcessExecutionStatus.Running;
        try
        {
            var startingBlockTriggers = request.Triggers
                ?? request.Process.DefaultBlockIds.Select(bId => new BlockTrigger(blockId: bId, triggerEvent: null));
            await RunTaskAsync(() =>
            {
                TriggerBlocks(blockTriggers: startingBlockTriggers, RunBlock);
                return Task.CompletedTask;
            });
            WaitForCompletion();
            Status = EProcessExecutionStatus.Completed;
        }
        catch (Exception ex)
        {
            Exception = ex;
            Status = EProcessExecutionStatus.Failed;
            throw;
        }
    }

    protected virtual void TriggerBlocks(
        IEnumerable<BlockTrigger> blockTriggers,
        Func<RunBlockRequest, IBlockExecutionControl, Task<BlockExecutionResult>> RunBlock)
    {
        foreach (var trigger in blockTriggers)
        {
            var block = Process.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
            if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
            _ = RunTaskAsync(async () =>
            {
                await WaitForCompletion(block.Id);
                var blockControl = GetOrInitBlockControl(block);
                var triggerEvent = trigger.TriggerEvent ?? block.Definition.DefaultTriggerEvent;
                await WaitAndPrepareInputs(triggerEvent, blockControl);
                var runRequest = new RunBlockRequest(block, triggerEvent);
                await ProcessBlock(runRequest, blockControl, RunBlock);
            });
        }
    }

    protected virtual async Task ProcessBlock(
        RunBlockRequest request,
        IBlockExecutionControl blockControl,
        Func<RunBlockRequest, IBlockExecutionControl, Task<BlockExecutionResult>> RunBlock)
    {
        var block = request.Block;
        var startTime = DateTime.UtcNow;
        var executionTask = RunBlock(request, blockControl);
        _executionTasks.Add(new(blockId: block.Id, startTime, executionTask));
        var blockResult = await executionTask;
        var nextBlockTriggers = Process.FindNextBlocks(block.Id, outputEvents: blockResult.OutputEvents);
        TriggerBlocks(nextBlockTriggers, RunBlock);
    }

    protected virtual async Task WaitAndPrepareInputs(string triggerEvent, IBlockExecutionControl blockControl)
    {
        var block = blockControl.Block;
        var inputEvent = block.Definition.InputEvents.FirstOrDefault(ev => ev.Name == triggerEvent);
        if (inputEvent == null) throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var eventDataConnections = Process.DataConnections
            .Where(c => c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName));
        foreach (var connection in eventDataConnections)
        {
            switch (connection.Source)
            {
                case EDataSource.External:
                    {
                        var binding = _context.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.VariableName == connection.VariableName);
                        if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                        blockControl.GetInput(connection.VariableName).Value = binding.Value;
                        break;
                    }

                case EDataSource.Internal:
                    {
                        if (connection.SourceBlockId != null && connection.SourceVariableName != null)
                        {
                            await WaitForCompletion(connection.SourceBlockId);
                            var sourceBlock = Process.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId);
                            var sourceBlockControl = GetOrInitBlockControl(sourceBlock);
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
}