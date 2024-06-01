using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class CompositeEC<TFramework> : BaseEC<TFramework, CompositeBlockDef>, ICompositeEC, IDisposable where TFramework : IBlockFramework
{
    private readonly IBlockRunner _blockRunner;
    private ConcurrentDictionary<string, IExecutionControl> _blockExecControlMap;
    private readonly ConcurrentBag<string> _outputEvents;
    private int _runningTasksCount;

    public override event EventHandler Running;
    public override event EventHandler<Exception> Failed;
    public override event EventHandler Completed;

    private BlockExecutionResult _result;
    public override BlockExecutionResult Result => _result;

    public CompositeEC(
        BlockInstance block,
        CompositeBlockDef definition,
        IBlockRunner blockRunner,
        IFunctionRunner<TFramework> functionRunner,
        IBlockFrameworkFactory<TFramework> blockFrameworkFactory) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
        _blockRunner = blockRunner;
        _runningTasksCount = 0;
        _blockExecControlMap = new();
        _outputEvents = new();
    }

    public override Task Execute(string triggerEvent, IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        lock (_idleWait)
        {
            if (!IsIdle) throw new InvalidOperationException("Not ready for execute!");
            _idleWait.Reset();
        }

        triggerEvent ??= Definition.DefaultTriggerEvent;
        try
        {
            Status = EBlockExecutionStatus.Running;
            _outputEvents.Clear();
            Running?.Invoke(this, EventArgs.Empty);
            PrepareStates(bindings);
            var startingTriggers = Definition.FindNextBlocks(triggerEvent);
            TriggerBlocks(blockTriggers: startingTriggers, optimizationScopeId, cancellationToken);
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
        Status = EBlockExecutionStatus.Failed;
        Failed?.Invoke(this, ex);
    }

    protected virtual void TriggerBlocks(IEnumerable<BlockTrigger> blockTriggers, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        foreach (var trigger in blockTriggers)
        {
            if (trigger.BlockId != null)
            {
                var block = Definition.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
                if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
                _ = RunTaskAsync(async (cancellationToken) =>
                {
                    var execControl = GetOrInitExecutionControl(block);
                    var triggerEvent = trigger.TriggerEvent ?? Definition.GetDefinition(block.DefinitionId).DefaultTriggerEvent;
                    var blockBindings = PrepareBindings(triggerEvent, block, cancellationToken);
                    var runRequest = new RunBlockRequest(blockBindings, triggerEvent);
                    await _blockRunner.Run(runRequest, execControl, optimizationScopeId, cancellationToken);
                    var nextBlockTriggers = Definition.FindNextBlocks(block.Id, outputEvents: execControl.Result.OutputEvents);
                    TriggerBlocks(nextBlockTriggers, optimizationScopeId, cancellationToken);
                }, cancellationToken);
            }
            else _outputEvents.Add(trigger.TriggerEvent);
        }
    }

    protected virtual IEnumerable<VariableBinding> PrepareBindings(
        string triggerEvent, BlockInstance block, CancellationToken cancellationToken)
    {
        var bindings = new List<VariableBinding>();
        var inputEvent = Definition.GetDefinition(block.DefinitionId).Events.FirstOrDefault(ev => ev.Name == triggerEvent && ev.IsInput);
        if (inputEvent == null) throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var usingDataConnections = Definition.DataConnections
            .Where(c => c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName) && c.BindingType == EBindingType.Input)
            .ToArray();
        var blockBindings = new List<VariableBinding>();
        foreach (var connection in usingDataConnections)
        {
            if (connection.SourceVariableName == null)
                throw new ArgumentException("Invalid connection!");

            object value;
            if (connection.SourceBlockId != null)
            {
                var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                    ?? throw new KeyNotFoundException(connection.SourceBlockId);
                var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
                sourceExecControl.WaitForIdle(cancellationToken);
                var outputValue = sourceExecControl.GetOutput(connection.SourceVariableName);
                outputValue.WaitValueSet(cancellationToken);
                value = outputValue.Value;
            }
            else
            {
                var valueObject = GetValueObject(connection.SourceVariableName, connection.BindingType.ToVariableType());
                value = valueObject.Value;
            }

            blockBindings.Add(new(
                variableName: connection.VariableName,
                value: value,
                type: connection.BindingType));
        }
        return blockBindings;
    }

    protected virtual void RefreshOutputs()
    {
        var outputEvents = Definition.Events.Where(e => !e.IsInput && _result.OutputEvents.Contains(e.Name));
        var allVariableNames = outputEvents.SelectMany(ev => ev.VariableNames);
        var usingDataConnections = Definition.DataConnections
            .Where(c => allVariableNames.Contains(c.VariableName) && c.BindingType == EBindingType.Output)
            .ToArray();
        var outputValues = new List<IValueObject>();

        foreach (var connection in usingDataConnections)
        {
            if (connection.SourceBlockId == null || connection.SourceVariableName == null)
                throw new ArgumentException("Invalid connection!");

            var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                ?? throw new KeyNotFoundException(connection.SourceBlockId);
            var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
            var outputValue = sourceExecControl.GetOutput(connection.SourceVariableName);
            var valueObject = GetOutput(connection.VariableName);
            valueObject.TempValue = outputValue.Value;
            outputValues.Add(valueObject);
        }

        foreach (var outputValue in outputValues)
            outputValue.TryCommit();
    }

    protected virtual void StartTask()
    {
        lock (_idleWait) { _runningTasksCount++; }
    }

    protected virtual void CompleteTask()
    {
        bool cfbCompleted = false;
        lock (_idleWait)
        {
            if (_runningTasksCount > 0 && --_runningTasksCount == 0)
            {
                _idleWait.Set();
                if (Status == EBlockExecutionStatus.Running)
                {
                    _result = new BlockExecutionResult(_outputEvents.ToArray());
                    RefreshOutputs();
                    Status = EBlockExecutionStatus.Completed;
                    cfbCompleted = true;
                }
            }
        }
        if (cfbCompleted) Completed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryGetExecutionControl(string blockId, out IExecutionControl execControl)
    {
        execControl = null;
        if (_blockExecControlMap.TryGetValue(blockId, out var targetExecControl))
            execControl = targetExecControl;
        return execControl != null;
    }

    protected virtual IExecutionControl GetOrInitExecutionControl(BlockInstance block)
        => _blockExecControlMap.GetOrAdd(block.Id, (key) =>
        {
            var definition = Definition.GetDefinition(block.DefinitionId);
            if (definition is BasicBlockDef basicBlockDef)
                return new BasicEC<TFramework>(block, definition: basicBlockDef, _functionRunner, _blockFrameworkFactory);
            else if (definition is CompositeBlockDef compositeBlockDef)
                return new CompositeEC<TFramework>(block, definition: compositeBlockDef, _blockRunner, _functionRunner, _blockFrameworkFactory);
            else throw new NotSupportedException($"Definition of type {definition.GetType().FullName} not supported!");
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

}