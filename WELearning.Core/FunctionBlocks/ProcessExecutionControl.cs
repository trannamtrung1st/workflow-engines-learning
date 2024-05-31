using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessExecutionControl<TFramework> : IProcessExecutionControl, IDisposable where TFramework : IBlockFramework
{
    private readonly IBlockRunner<TFramework> _blockRunner;
    private readonly ILogicRunner<TFramework> _logicRunner;
    private readonly IBlockFrameworkFactory<TFramework> _blockFrameworkFactory;
    private readonly SemaphoreSlim _mutexLock;
    private readonly ManualResetEventSlim _idleWait;
    private ConcurrentDictionary<string, BlockExecutionControl<TFramework>> _blockExecutionControlMap;
    private readonly ConcurrentBag<BlockExecutionTaskInfo> _executionTasks;
    private int _runningTasksCount;

    public ProcessExecutionControl(FunctionBlockProcess process,
        IBlockRunner<TFramework> blockRunner,
        ILogicRunner<TFramework> logicRunner,
        IBlockFrameworkFactory<TFramework> blockFrameworkFactory)
    {
        _blockRunner = blockRunner;
        _logicRunner = logicRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _runningTasksCount = 0;
        _blockExecutionControlMap = new();
        _idleWait = new(initialState: true);
        _mutexLock = new(1);
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
    public virtual bool IsIdle => _idleWait.IsSet;

    protected virtual void StartTask()
    {
        lock (_idleWait) { _runningTasksCount++; }
    }

    protected virtual void CompleteTask()
    {
        bool processCompleted = false;
        lock (_idleWait)
        {
            if (_runningTasksCount > 0 && --_runningTasksCount == 0)
            {
                _idleWait.Set();
                if (Status == EProcessExecutionStatus.Running)
                {
                    Status = EProcessExecutionStatus.Completed;
                    processCompleted = true;
                }
            }
        }
        if (processCompleted) Completed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryGetBlockControl(string blockId, out IBlockExecutionControl blockControl)
    {
        blockControl = null;
        if (_blockExecutionControlMap.TryGetValue(blockId, out var targetBlockControl))
            blockControl = targetBlockControl;
        return blockControl != null;
    }

    public virtual IBlockExecutionControl GetOrInitBlockControl(FunctionBlockInstance block)
        => _blockExecutionControlMap.GetOrAdd(block.Id, (key)
            => new BlockExecutionControl<TFramework>(block, definition: Process.GetDefinition(block.DefinitionId), _logicRunner, _blockFrameworkFactory));

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

    public virtual Task Execute(IEnumerable<BlockTrigger> triggers, IEnumerable<ProcessVariableBinding> bindings, CancellationToken cancellationToken)
    {
        lock (_idleWait)
        {
            if (!IsIdle) throw new InvalidOperationException("Not ready for execute!");
            _idleWait.Reset();
        }
        try
        {
            Status = EProcessExecutionStatus.Running;
            Running?.Invoke(this, EventArgs.Empty);
            PrepareStates(bindings);
            var startingBlockTriggers = triggers
                ?? Process.DefaultBlockIds.Select(bId => new BlockTrigger(blockId: bId, triggerEvent: null));
            TriggerBlocks(blockTriggers: startingBlockTriggers, processBindings: bindings, cancellationToken);
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

    protected virtual void TriggerBlocks(IEnumerable<BlockTrigger> blockTriggers,
        IEnumerable<ProcessVariableBinding> processBindings, CancellationToken cancellationToken)
    {
        foreach (var trigger in blockTriggers)
        {
            var block = Process.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
            if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
            _ = RunTaskAsync(async (cancellationToken) =>
            {
                var blockControl = GetOrInitBlockControl(block);
                var triggerEvent = trigger.TriggerEvent ?? Process.GetDefinition(block.DefinitionId).DefaultTriggerEvent;
                var blockBindings = PrepareBindings(triggerEvent, processBindings, block, cancellationToken);
                var runRequest = new RunBlockRequest(block, blockBindings, triggerEvent);
                var blockResult = await ProcessBlock(runRequest, blockControl, cancellationToken);
                var nextBlockTriggers = Process.FindNextBlocks(block.Id, outputEvents: blockResult.OutputEvents);
                TriggerBlocks(nextBlockTriggers, processBindings, cancellationToken);
            }, cancellationToken);
        }
    }

    protected virtual async Task<BlockExecutionResult> ProcessBlock(
        RunBlockRequest request, IBlockExecutionControl blockControl, CancellationToken cancellationToken)
    {
        var block = request.Block;
        var startTime = DateTime.UtcNow;
        var executionTask = _blockRunner.Run(request, blockControl, optimizationScopeId: default, cancellationToken);
        _executionTasks.Add(new(blockId: block.Id, startTime, executionTask));
        var blockResult = await executionTask;
        return blockResult;
    }

    protected virtual void PrepareStates(IEnumerable<ProcessVariableBinding> processBindings)
    {
        foreach (var binding in processBindings)
        {
            var block = Process.Blocks.FirstOrDefault(b => b.Id == binding.BlockId);
            var blockControl = GetOrInitBlockControl(block);
            var blockBinding = binding.Binding;
            var valueObject = blockControl.GetValueObject(key: blockBinding.VariableName, type: blockBinding.Type.ToVariableType());
            valueObject.Value = blockBinding.Value;
        }
    }

    protected virtual IEnumerable<VariableBinding> PrepareBindings(string triggerEvent,
        IEnumerable<ProcessVariableBinding> processBindings,
        FunctionBlockInstance block, CancellationToken cancellationToken)
    {
        var bindings = new List<VariableBinding>();
        var inputEvent = Process.GetDefinition(block.DefinitionId).Events.FirstOrDefault(ev => ev.Name == triggerEvent && ev.IsInput);
        if (inputEvent == null) throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var usingDataConnections = Process.DataConnections
            .Where(c => c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName))
            .ToArray();
        var externalBindings = processBindings.Where(b => b.BlockId == block.Id).ToArray();
        var blockBindings = new List<VariableBinding>();
        foreach (var connection in usingDataConnections)
        {
            if (connection.SourceBlockId == null || connection.SourceVariableName == null)
                throw new ArgumentException("Invalid connection!");
            var sourceBlock = Process.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                ?? throw new KeyNotFoundException(connection.SourceBlockId);
            var sourceBlockControl = GetOrInitBlockControl(sourceBlock);
            sourceBlockControl.WaitForIdle(cancellationToken);
            var outputValue = sourceBlockControl.GetOutput(connection.SourceVariableName);
            outputValue.WaitValueSet(cancellationToken);
            blockBindings.Add(new(
                variableName: connection.VariableName,
                value: outputValue.Value,
                type: EBindingType.Input));
        }
        return blockBindings;
    }

    public virtual void WaitForIdle(CancellationToken cancellationToken) => _idleWait.Wait(cancellationToken);

    public virtual async Task MutexAccess(Func<Task> Task, CancellationToken cancellationToken)
    {
        await _mutexLock.WaitAsync(cancellationToken);
        try { await Task(); }
        finally { _mutexLock.Release(); }
    }

    public void Dispose()
    {
        _mutexLock.Dispose();
        _idleWait.Dispose();
    }
}