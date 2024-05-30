using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessExecutionControl : IProcessExecutionControl
{
    private readonly ProcessExecutionContext _context;
    private readonly ManualResetEventSlim _idleWait;
    private ConcurrentDictionary<string, BlockExecutionControl> _blockExecutionControlMap;
    private readonly ConcurrentBag<BlockExecutionTaskInfo> _executionTasks;
    private int _runningTasksCount;

    public ProcessExecutionControl(FunctionBlockProcess process, ProcessExecutionContext context)
    {
        _context = context;
        _runningTasksCount = 0;
        _blockExecutionControlMap = new();
        _idleWait = new(initialState: true);
        _executionTasks = new();
        Process = process;
    }

    public event EventHandler Running;
    public event EventHandler<Exception> Failed;
    public event EventHandler Completed;

    public FunctionBlockProcess Process { get; }
    public virtual int RunningTasksCount => _runningTasksCount;
    public virtual IEnumerable<BlockExecutionTaskInfo> ExecutionTasks => _executionTasks;
    public virtual Exception Exception { get; protected set; }
    public virtual EProcessExecutionStatus Status { get; protected set; }
    public virtual bool IsRunning => Status == EProcessExecutionStatus.Running;

    protected virtual void StartTask()
    {
        lock (_idleWait)
        {
            _runningTasksCount++;
        }
    }

    protected virtual void CompleteTask()
    {
        bool processCompleted = false;
        lock (_idleWait)
        {
            if (_runningTasksCount > 0 && --_runningTasksCount == 0 && IsRunning)
            {
                _idleWait.Set();
                if (IsRunning)
                {
                    Status = EProcessExecutionStatus.Completed;
                    processCompleted = true;
                }
            }
        }
        if (processCompleted) Completed?.Invoke(this, EventArgs.Empty);
    }

    public virtual void WaitForCompletion(CancellationToken cancellationToken) => _idleWait.Wait(cancellationToken);

    public bool TryGetBlockControl(string blockId, out IBlockExecutionControl blockControl)
    {
        blockControl = null;
        if (_blockExecutionControlMap.TryGetValue(blockId, out var targetBlockControl))
            blockControl = targetBlockControl;
        return blockControl != null;
    }

    public virtual IBlockExecutionControl GetOrInitBlockControl(FunctionBlockInstance block)
        => _blockExecutionControlMap.GetOrAdd(block.Id, (key) =>
        {
            var blockControl = new BlockExecutionControl(block);
            var outputVariables = block.Definition.Outputs.Select(o => o.Name).ToArray();
            var blockExternalOutputs = Process.DataConnections
                .Where(c => c.BlockId == block.Id && c.Source == EDataSource.External && outputVariables.Contains(c.VariableName));

            foreach (var connection in blockExternalOutputs)
            {
                var binding = _context.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.Binding.VariableName == connection.VariableName);
                if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                blockControl.GetOutput(connection.VariableName).Value = binding.Binding.Value;
            }

            return blockControl;
        });

    protected Task RunTaskAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken)
    {
        StartTask();
        return Task.Factory.StartNew(async () =>
        {
            try { await func(cancellationToken); }
            catch (Exception ex) { HandleFailed(ex); }
            finally { CompleteTask(); }
        }, creationOptions: TaskCreationOptions.LongRunning);
    }

    // [TODO] refactor runner and control
    public virtual Task Execute(RunProcessRequest request,
        Func<RunBlockRequest, IBlockExecutionControl, CancellationToken, Task<BlockExecutionResult>> RunBlock,
        CancellationToken cancellationToken)
    {
        WaitForCompletion(cancellationToken);
        _idleWait.Reset();
        try
        {
            Status = EProcessExecutionStatus.Running;
            Running?.Invoke(this, EventArgs.Empty);
            var startingBlockTriggers = request.Triggers
                ?? request.Process.DefaultBlockIds.Select(bId => new BlockTrigger(blockId: bId, triggerEvent: null));
            TriggerBlocks(blockTriggers: startingBlockTriggers, RunBlock, cancellationToken);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            HandleFailed(ex);
            throw;
        }
    }

    private void HandleFailed(Exception ex)
    {
        Exception = ex;
        Status = EProcessExecutionStatus.Failed;
        Failed?.Invoke(this, ex);
    }

    protected virtual void TriggerBlocks(
        IEnumerable<BlockTrigger> blockTriggers,
        Func<RunBlockRequest, IBlockExecutionControl, CancellationToken, Task<BlockExecutionResult>> RunBlock,
        CancellationToken cancellationToken)
    {
        foreach (var trigger in blockTriggers)
        {
            var block = Process.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
            if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
            _ = RunTaskAsync(async (cancellationToken) =>
            {
                var blockControl = GetOrInitBlockControl(block);
                blockControl.WaitForCompletion(cancellationToken);
                var triggerEvent = trigger.TriggerEvent ?? block.Definition.DefaultTriggerEvent;
                WaitAndPrepareBindings(triggerEvent, blockControl, cancellationToken);
                var runRequest = new RunBlockRequest(block, triggerEvent);
                await ProcessBlock(runRequest, blockControl, RunBlock, cancellationToken);
            }, cancellationToken);
        }
    }

    protected virtual async Task ProcessBlock(
        RunBlockRequest request,
        IBlockExecutionControl blockControl,
        Func<RunBlockRequest, IBlockExecutionControl, CancellationToken, Task<BlockExecutionResult>> RunBlock,
        CancellationToken cancellationToken)
    {
        var block = request.Block;
        var startTime = DateTime.UtcNow;
        var executionTask = RunBlock(request, blockControl, cancellationToken);
        _executionTasks.Add(new(blockId: block.Id, startTime, executionTask));
        var blockResult = await executionTask;
        var nextBlockTriggers = Process.FindNextBlocks(block.Id, outputEvents: blockResult.OutputEvents);
        TriggerBlocks(nextBlockTriggers, RunBlock, cancellationToken);
    }

    protected virtual void WaitAndPrepareBindings(string triggerEvent, IBlockExecutionControl blockControl, CancellationToken cancellationToken)
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
                        var binding = _context.Bindings.FirstOrDefault(b => b.BlockId == block.Id && b.Binding.VariableName == connection.VariableName);
                        if (binding == null) throw new KeyNotFoundException($"Binding for {connection.VariableName} not found!");
                        blockControl.GetInput(connection.VariableName).Value = binding.Binding.Value;
                        break;
                    }

                case EDataSource.Internal:
                    {
                        if (connection.SourceBlockId != null && connection.SourceVariableName != null)
                        {
                            var sourceBlock = Process.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId);
                            var sourceBlockControl = GetOrInitBlockControl(sourceBlock);
                            sourceBlockControl.WaitForCompletion(cancellationToken);
                            var outputValue = sourceBlockControl.GetOutput(connection.SourceVariableName);
                            outputValue.WaitValueSet(cancellationToken);
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