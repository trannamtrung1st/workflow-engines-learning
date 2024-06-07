using System.Collections.Concurrent;
using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class CompositeEC<TFunctionFramework> : BaseEC<CompositeBlockDef>, ICompositeEC, IDisposable
    where TFunctionFramework : class
{
    private readonly TFunctionFramework _functionFramework;
    private readonly IBlockRunner _blockRunner;
    private ConcurrentDictionary<string, IExecutionControl> _blockExecControlMap;
    private ConcurrentBag<string> _outputEvents;
    private int _runningTasksCount;

    public virtual IExecutionControl ExceptionFrom { get; protected set; }

    public override event EventHandler Running;
    public override event EventHandler<Exception> Failed;
    public override event EventHandler Completed;
    public event EventHandler ControlRunning;
    public event EventHandler ControlCompleted;
    public event EventHandler<Exception> ControlFailed;

    public CompositeEC(
        BlockInstance block,
        CompositeBlockDef definition,
        IBlockRunner blockRunner,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        TFunctionFramework functionFramework) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
        _functionFramework = functionFramework;
        _blockRunner = blockRunner;
        _runningTasksCount = 0;
        _blockExecControlMap = new();
        _outputEvents = new();
    }

    public override Task Execute(RunBlockRequest request, Guid? optimizationScopeId)
    {
        EnterOrThrow();
        var triggerEvent = GetTriggerOrDefault(request.TriggerEvent);
        try
        {
            _outputEvents = new();
            PrepareRunningStatus(request);
            Running?.Invoke(this, EventArgs.Empty);
            PrepareStates(request.Bindings);
            var startingTriggers = Definition.FindNextBlocks(triggerEvent);
            TriggerBlocks(blockTriggers: startingTriggers, optimizationScopeId);
        }
        catch (Exception ex)
        {
            HandleFailed(ex);
            throw;
        }
        return Task.CompletedTask;
    }

    protected override void PrepareRunningStatus(RunBlockRequest runRequest)
    {
        ExceptionFrom = null;
        base.PrepareRunningStatus(runRequest);
    }

    protected override void PrepareFailedStatus(Exception ex, IExecutionControl from = null)
    {
        ExceptionFrom = from ?? this;
        base.PrepareFailedStatus(ex, from);
    }

    protected void HandleFailed(Exception ex, IExecutionControl from = null)
    {
        if (!SetFailedStatus()) return;
        PrepareFailedStatus(ex, from);
        Failed?.Invoke(this, ex);
    }

    protected virtual void TriggerBlocks(IEnumerable<BlockTrigger> blockTriggers, Guid? optimizationScopeId)
    {
        foreach (var trigger in blockTriggers)
        {
            if (trigger.BlockId != null)
            {
                var block = Definition.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId);
                if (block == null) throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
                _ = RunTaskAsync(async () =>
                {
                    var execControl = GetOrInitExecutionControl(block);
                    var triggerEvent = trigger.TriggerEvent ?? Definition.GetDefinition(block.DefinitionId).DefaultTriggerEvent;
                    var blockBindings = PrepareBindings(triggerEvent, block);
                    var runRequest = new RunBlockRequest(
                        runId: CurrentRunRequest.RunId, blockBindings,
                        tokens: CurrentRunRequest.Tokens,
                        triggerEvent: triggerEvent);
                    await _blockRunner.Run(runRequest, execControl, optimizationScopeId);
                    var nextBlockTriggers = Definition.FindNextBlocks(block.Id, outputEvents: execControl.Result.OutputEvents);
                    TriggerBlocks(nextBlockTriggers, optimizationScopeId);
                });
            }
            else _outputEvents.Add(trigger.TriggerEvent);
        }
    }

    protected virtual IEnumerable<VariableBinding> PrepareBindings(string triggerEvent, BlockInstance block)
    {
        var bindings = new List<VariableBinding>();
        var inputEvent = Definition.GetDefinition(block.DefinitionId).Events.FirstOrDefault(ev => ev.Name == triggerEvent && ev.IsInput);
        if (inputEvent == null) throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var blockExecControl = GetOrInitExecutionControl(block);
        var blockBindings = new List<VariableBinding>();

        var blockReferences = Definition.References?.Where(r => r.BlockId == block.Id);
        if (blockReferences?.Any() == true)
        {
            foreach (var reference in blockReferences)
            {
                if (reference.SourceVariableName == null)
                    throw new ArgumentException("Invalid reference!");
                var variableType = reference.BindingType.ToVariableType();
                var bindingVariable = blockExecControl.GetVariable(key: reference.VariableName, type: variableType)
                    ?? throw new KeyNotFoundException($"Variable {reference.VariableName} not found!");
                if (bindingVariable.DataType != EDataType.Reference)
                    throw new InvalidOperationException($"Cannot bind non-reference type {bindingVariable.DataType}");

                var sourceValue = GetValueObject(reference.SourceVariableName, variableType);
                blockBindings.Add(new(
                    variableName: reference.VariableName,
                    reference: sourceValue.CloneFor(bindingVariable),
                    type: reference.BindingType));
            }
        }

        var inputDataConnections = Definition.DataConnections
            .Where(c => c.BindingType == EBindingType.Input && c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName))
            .ToArray();
        foreach (var connection in inputDataConnections)
        {
            if (connection.SourceVariableName == null)
                throw new ArgumentException("Invalid connection!");

            var bindingVariable = blockExecControl.GetVariable(key: connection.VariableName, type: EVariableType.Input)
                ?? throw new KeyNotFoundException($"Variable {connection.VariableName} not found!");

            object value = null; IValueObject reference = null; IValueObject sourceValue;
            if (connection.SourceBlockId != null)
            {
                var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                    ?? throw new KeyNotFoundException($"Block {connection.SourceBlockId} not found!");
                var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
                sourceExecControl.WaitForIdle(CurrentRunRequest.Tokens.Combined);
                var outputValue = sourceExecControl.GetOutput(connection.SourceVariableName);
                outputValue.WaitValueSet(CurrentRunRequest.Tokens.Combined);
                sourceValue = outputValue;
            }
            else
            {
                sourceValue = GetInput(connection.SourceVariableName);
            }

            if (bindingVariable.DataType == EDataType.Reference
                && !blockBindings.Any(b => b.VariableName == connection.VariableName && b.Reference != null))
                reference = sourceValue.CloneFor(bindingVariable);
            else value = sourceValue.ValueSet ? sourceValue.Value : bindingVariable.DefaultValue;

            blockBindings.Add(new(
                variableName: connection.VariableName,
                value: value,
                reference: reference,
                type: EBindingType.Input));
        }

        return blockBindings;
    }

    protected override void RefreshOutputs()
    {
        var outputEvents = Definition.Events.Where(e => !e.IsInput && Result.OutputEvents.Contains(e.Name));
        var allVariableNames = outputEvents.SelectMany(ev => ev.VariableNames);
        var usingDataConnections = Definition.DataConnections
            .Where(c => c.BlockId == null && allVariableNames.Contains(c.VariableName) && c.BindingType == EBindingType.Output)
            .ToArray();
        var outputValues = new List<IValueObject>();

        foreach (var connection in usingDataConnections)
        {
            if (connection.SourceBlockId == null || connection.SourceVariableName == null)
                throw new ArgumentException("Invalid connection!");

            var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                ?? throw new KeyNotFoundException($"Source block {connection.SourceBlockId} not found!");
            var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
            var sourceValue = sourceExecControl.GetOutput(connection.SourceVariableName);

            IValueObject valueObject = GetOutput(connection.VariableName);
            valueObject.TempValue = sourceValue.Value;
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
                if (Status == EBlockExecutionStatus.Running)
                {
                    Result = new BlockExecutionResult(_outputEvents);
                    PrepareCompletedStatus();
                    cfbCompleted = true;
                }
                _idleWait.Set();
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
            IExecutionControl execControl;
            if (definition is BasicBlockDef basicBlockDef)
            {
                var importBlocks = basicBlockDef.ImportBlockIds?
                    .Select(bId => Definition.GetDefinition(bId))
                    .OfType<BasicBlockDef>().ToArray();
                execControl = new BasicEC<TFunctionFramework>(block, definition: basicBlockDef, importBlocks: importBlocks, _functionRunner, _blockFrameworkFactory, _functionFramework);
            }
            else if (definition is CompositeBlockDef compositeBlockDef)
                execControl = new CompositeEC<TFunctionFramework>(block, definition: compositeBlockDef, _blockRunner, _functionRunner, _blockFrameworkFactory, _functionFramework);
            else throw new NotSupportedException($"Definition of type {definition.GetType().FullName} not supported!");
            execControl.Running += HandleControlRunning;
            execControl.Completed += HandleControlCompleted;
            execControl.Failed += HandleControlFailed;
            return execControl;
        });

    protected virtual void HandleControlRunning(object sender, EventArgs ev) => ControlRunning?.Invoke(sender, ev);

    protected virtual void HandleControlCompleted(object sender, EventArgs ev) => ControlCompleted?.Invoke(sender, ev);

    protected virtual void HandleControlFailed(object sender, Exception ex)
    {
        ControlFailed?.Invoke(sender, ex);
        HandleFailed(ex, sender as IExecutionControl);
    }

    protected Task RunTaskAsync(Func<Task> func)
    {
        StartTask();
        return Task.Factory.StartNew(async () =>
        {
            try { await func(); }
            catch (Exception ex) { HandleFailed(ex); }
            finally { CompleteTask(); }
        }, creationOptions: TaskCreationOptions.LongRunning);
    }

}