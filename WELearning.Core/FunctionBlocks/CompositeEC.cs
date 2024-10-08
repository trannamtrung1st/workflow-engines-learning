using System.Collections.Concurrent;
using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Models;
using TNT.Boilerplates.Concurrency.Abstracts;
using TNT.Boilerplates.Common.Disposable;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.Core.Common.Extensions;

namespace WELearning.Core.FunctionBlocks;

public class CompositeEC<TFunctionFramework> : BaseEC<CompositeBlockDef>, ICompositeEC, IDisposable
    where TFunctionFramework : IFunctionFramework
{
    private readonly IFunctionFrameworkFactory<TFunctionFramework> _functionFrameworkFactory;
    private readonly IBlockRunner _blockRunner;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ISyncAsyncTaskLimiter _taskLimiter;
    private ConcurrentDictionary<string, IExecutionControl> _blockExecControlMap;
    private ConcurrentBag<string> _outputEvents;
    private CancellationTokenSource _taskLoopCts;
    private readonly ManualResetEventSlim _taskLoopEvent;
    private readonly ConcurrentQueue<Func<Task>> _tasks;
    private int _taskCount = 0;

    public virtual IExecutionControl ExceptionFrom { get; protected set; }

    public event EventHandler ControlRunning;
    public event EventHandler ControlCompleted;
    public event EventHandler<Exception> ControlFailed;

    public CompositeEC(
        BlockInstance block,
        CompositeBlockDef definition,
        IBlockRunner blockRunner,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        IFunctionFrameworkFactory<TFunctionFramework> functionFrameworkFactory,
        ISyncAsyncTaskRunner taskRunner,
        ISyncAsyncTaskLimiter taskLimiter,
        bool printErrorLocation = false) : base(block, definition, functionRunner, blockFrameworkFactory, printErrorLocation)
    {
        _functionFrameworkFactory = functionFrameworkFactory;
        _blockRunner = blockRunner;
        _taskRunner = taskRunner;
        _taskLimiter = taskLimiter;
        _taskCount = 0;
        _taskLoopEvent = new();
        _blockExecControlMap = new();
        _outputEvents = new();
        _tasks = new();
    }

    public override async Task Execute(RunBlockRequest request, string optimizationScopeId)
    {
        EnterOrThrow();
        try
        {
            HandleRunning(request);

            var triggerEvent = GetTriggerOrDefault(request.TriggerEvent);
            var startingTriggers = Definition.FindNextBlocks(triggerEvent);
            EnqueueTask((taskScope) => TriggerBlocks(blockTriggers: startingTriggers, optimizationScopeId, taskScope, tokens: request.Tokens));

            using var waitToken = CancellationTokenSource.CreateLinkedTokenSource(_taskLoopCts.Token, request.Tokens.Combined);
            while (_taskCount > 0)
            {
                _taskLoopEvent.Reset();
                while (_tasks.TryDequeue(out var task))
                    await task();
                WaitForTasks(cancellationToken: waitToken.Token);
            }

            Result = new BlockExecutionResult(_outputEvents);
            HandleCompleted();
        }
        catch (Exception ex)
        {
            HandleException(ex);
            throw;
        }
        finally
        {
            _taskLoopCts?.Dispose();
            SetIdle();
        }
    }

    protected override void PrepareStates(IEnumerable<VariableBinding> bindings)
    {
        base.PrepareStates(bindings);

        var initialBlockReferences = Definition.References?.Where(r => r.SourceBlockId == null);
        if (initialBlockReferences?.Any() == true)
        {
            foreach (var connection in initialBlockReferences)
            {
                if (connection.SourceVariableName == null)
                    throw new ArgumentException("Invalid reference!");
                var block = Definition.Blocks.FirstOrDefault(b => b.Id == connection.BlockId)
                    ?? throw new KeyNotFoundException($"Block {connection.BlockId} not found!");
                var blockExecControl = GetOrInitExecutionControl(block);
                var variableType = connection.BindingType.ToVariableType();
                var bindingVariable = blockExecControl.GetVariable(key: connection.VariableName, type: variableType)
                    ?? throw new KeyNotFoundException($"Variable {connection.VariableName} not found!");
                if (bindingVariable.DataType != EDataType.Reference)
                    throw new InvalidOperationException($"Cannot bind non-reference type {bindingVariable.DataType}");

                IValueObject sourceReference = GetValueObject(connection.SourceVariableName, variableType);
                blockExecControl.SetReference(
                    bindingVariable.Name,
                    bindingVariable.VariableType,
                    reference: sourceReference.CloneFor(bindingVariable));
            }
        }
    }

    protected override void PrepareRunningStatus(RunBlockRequest request)
    {
        _outputEvents = new();
        ExceptionFrom = null;
        _taskLoopCts = new();
        _taskLoopEvent.Reset();
        base.PrepareRunningStatus(request);
    }

    protected override void PrepareFailedStatus(Exception ex, IExecutionControl from = null)
    {
        ExceptionFrom = from ?? this;
        base.PrepareFailedStatus(ex, from);
    }

    protected virtual async Task TriggerBlocks(IEnumerable<BlockTrigger> blockTriggers, string optimizationScopeId, IDisposable taskScope, RunTokens tokens)
    {
        using var _ = taskScope;
        foreach (var trigger in blockTriggers)
        {
            if (trigger.BlockId != null)
            {
                var block = Definition.Blocks.FirstOrDefault(b => b.Id == trigger.BlockId)
                    ?? throw new KeyNotFoundException($"Block {trigger.BlockId} not found!");
                var blockDef = Definition.GetDefinition(block.DefinitionId) as BasicBlockDef;

                async Task TriggerBlock(IDisposable taskScope)
                {
                    try
                    {
                        using var _ = taskScope;
                        var execControl = GetOrInitExecutionControl(block);
                        var triggerEvent = trigger.TriggerEvent ?? Definition.GetDefinition(block.DefinitionId).DefaultTriggerEvent;
                        var reservedInputs = CurrentRunRequest.ReservedInputs;
                        var blockBindings = await PrepareBindings(triggerEvent, block, onDelayed: EnqueueTriggerBlock, reservedInputs, tokens, optimizationScopeId);
                        var runRequest = new RunBlockRequest(
                            runId: CurrentRunRequest.RunId, blockBindings,
                            tokens: CurrentRunRequest.Tokens,
                            triggerEvent: triggerEvent,
                            reservedInputs: reservedInputs,
                            optimizationScopes: CurrentRunRequest.OptimizationScopes);
                        var controlScopeId = GetControlScopeId(optimizationScopeId, execControl);
                        await RunBlock(runRequest, execControl, optimizationScopeId: controlScopeId);
                        var nextBlockTriggers = Definition.FindNextBlocks(block.Id, outputEvents: execControl.Result.OutputEvents);
                        EnqueueTask((taskScope) => TriggerBlocks(nextBlockTriggers, optimizationScopeId, taskScope, tokens));
                    }
                    catch (BlockDelayedException) { }
                }

                Task EnqueueTriggerBlock()
                {
                    EnqueueTask(blockDef.HasAsyncFunction
                        ? (scope) => TryRunTaskAsync(task: () => TriggerBlock(scope))
                        : TriggerBlock);
                    return Task.CompletedTask;
                }

                await EnqueueTriggerBlock();
            }
            else _outputEvents.Add(trigger.TriggerEvent);
        }
    }

    protected virtual string GetControlScopeId(string optimizationScopeId, IExecutionControl execControl)
        => $"{optimizationScopeId}_{execControl.Block.Id}";

    protected virtual async Task RunBlock(RunBlockRequest runRequest, IExecutionControl execControl, string optimizationScopeId)
        => await _blockRunner.Run(runRequest, execControl, optimizationScopeId);

    protected virtual async Task<IEnumerable<VariableBinding>> PrepareBindings(string triggerEvent, BlockInstance block, Func<Task> onDelayed, IReadOnlyDictionary<string, object> reservedInputs, RunTokens tokens, string optimizationScopeId)
    {
        // [OPT] force set snapshot value before each BFB run to prevent concurrent modification of underlying reference object while BFB running
        var bindings = new List<VariableBinding>();
        var inputEvent = Definition.GetDefinition(block.DefinitionId).Events.FirstOrDefault(ev => ev.Name == triggerEvent && ev.IsInput)
            ?? throw new KeyNotFoundException($"Trigger event {triggerEvent} not found!");
        var blockExecControl = GetOrInitExecutionControl(block);
        var blockBindings = new List<VariableBinding>();

        var blockReferences = Definition.References?.Where(r => r.BlockId == block.Id);
        if (blockReferences?.Any() == true)
        {
            foreach (var connection in blockReferences)
            {
                if (connection.SourceVariableName == null)
                    throw new ArgumentException("Invalid reference!");
                var variableType = connection.BindingType.ToVariableType();
                var bindingVariable = blockExecControl.GetVariable(key: connection.VariableName, type: variableType)
                    ?? throw new KeyNotFoundException($"Variable {connection.VariableName} not found!");
                if (bindingVariable.DataType != EDataType.Reference)
                    throw new InvalidOperationException($"Cannot bind non-reference type {bindingVariable.DataType}");

                IValueObject sourceReference;
                if (connection.SourceBlockId != null)
                {
                    var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.SourceBlockId)
                        ?? throw new KeyNotFoundException($"Block {connection.SourceBlockId} not found!");
                    var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
                    if (sourceExecControl.RegisterTempIdleCallback(onDelayed))
                        throw new BlockDelayedException();

                    var sourceVariableType = variableType == EVariableType.Input ? EVariableType.Output : EVariableType.Input;
                    sourceReference = sourceExecControl.GetValueObject(connection.SourceVariableName, sourceVariableType);
                }
                else
                {
                    sourceReference = GetValueObject(connection.SourceVariableName, variableType);
                }

                blockBindings.Add(new(
                    variableName: connection.VariableName,
                    reference: sourceReference.CloneFor(bindingVariable),
                    type: connection.BindingType));
            }
        }

        optimizationScopeId ??= Guid.NewGuid().ToString();
        var optimizationScopes = CurrentRunRequest.OptimizationScopes ?? new Dictionary<string, IOptimizationScope>();
        try
        {
            var inputDataConnections = Definition.DataConnections
                .Where(c => c.BindingType == EBindingType.Input && c.BlockId == block.Id && inputEvent.VariableNames.Contains(c.VariableName))
                .ToArray();
            var lazyArguments = new Lazy<Dictionary<string, object>>(() =>
            {
                var arguments = new Dictionary<string, object>();
                if (blockExecControl is IBasicEC basicEC)
                {
                    arguments[basicEC.FunctionFramework.VariableName] = basicEC.FunctionFramework;
                    basicEC.BlockFramework.GetReservedInputs()?.AssignTo(arguments);
                }
                reservedInputs?.AssignTo(arguments);
                return arguments;
            });

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
                    if (sourceExecControl.RegisterTempIdleCallback(onDelayed))
                        throw new BlockDelayedException();

                    var outputValue = sourceExecControl.GetOutput(connection.SourceVariableName);
                    if (outputValue.RegisterTempValueSet(onDelayed))
                        throw new BlockDelayedException();
                    sourceValue = outputValue;
                }
                else sourceValue = GetInput(connection.SourceVariableName);

                if (bindingVariable.DataType == EDataType.Reference
                    && !blockBindings.Any(b => b.VariableName == connection.VariableName && b.Reference != null))
                    reference = sourceValue.CloneFor(bindingVariable);
                else
                {
                    if (sourceValue.ValueSet)
                        value = connection.SourceProperty != null
                            ? sourceValue.GetProperty(connection.SourceProperty)
                            : sourceValue.Value;
                    else
                        value = bindingVariable.DefaultValue;
                    value.As(bindingVariable.DataType, out value);

                    if (connection.Preprocessing != null)
                    {
                        var (result, scope) = await _functionRunner.Evaluate<object, Dictionary<string, object>>(
                            function: connection.Preprocessing, tracker: CurrentRunRequest.Tracker,
                            arguments: new(lazyArguments.Value)
                            {
                                [BuiltInVariables.THIS] = value
                            }, optimizationScopeId, tokens);
                        value = result;
                        optimizationScopes[scope.Id] = scope;
                    }
                }

                blockBindings.Add(new(
                    variableName: connection.VariableName,
                    value: value,
                    reference: reference,
                    type: EBindingType.Input));
            }
        }
        finally
        {
            if (CurrentRunRequest.OptimizationScopes is null)
                foreach (var optimizationScope in optimizationScopes.Values)
                    optimizationScope.Dispose();
        }

        return blockBindings;
    }

    protected override void RefreshOutputs()
    {
        if (Result?.OutputEvents.Any() != true)
            return;

        var outputEvents = Definition.Events.Where(e => !e.IsInput && Result.OutputEvents.Contains(e.Name));
        var allVariableNames = outputEvents.SelectMany(ev => ev.VariableNames);
        var usingDataConnections = Definition.DataConnections?
            .Where(c => c.BlockId == null && allVariableNames.Contains(c.VariableName) && c.BindingType == EBindingType.Output)
            .ToArray() ?? [];
        var usingReferences = Definition.References?
            .Where(c => c.SourceBlockId == null && allVariableNames.Contains(c.SourceVariableName) && c.BindingType == EBindingType.Output)
            .ToArray() ?? [];
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

        foreach (var connection in usingReferences)
        {
            if (connection.BlockId == null || connection.VariableName == null)
                throw new ArgumentException("Invalid connection!");

            var sourceBlock = Definition.Blocks.FirstOrDefault(b => b.Id == connection.BlockId)
                ?? throw new KeyNotFoundException($"Source block {connection.BlockId} not found!");
            var sourceExecControl = GetOrInitExecutionControl(sourceBlock);
            var sourceValue = sourceExecControl.GetOutput(connection.VariableName);

            IValueObject valueObject = GetOutput(connection.SourceVariableName);
            valueObject.TempValue = sourceValue.Value;
            outputValues.Add(valueObject);
        }

        foreach (var outputValue in outputValues)
            outputValue.TryCommit();
    }

    public bool TryGetExecutionControl(string blockId, out IExecutionControl execControl)
    {
        execControl = null;
        if (_blockExecControlMap.TryGetValue(blockId, out var targetExecControl))
            execControl = targetExecControl;
        return execControl != null;
    }

    protected virtual IExecutionControl GetOrInitExecutionControl(BlockInstance blockInstance)
        => _blockExecControlMap.GetOrAdd(blockInstance.Id, (key) =>
        {
            var definition = Definition.GetDefinition(blockInstance.DefinitionId);
            IExecutionControl execControl;
            if (definition is BasicBlockDef basicBlockDef)
            {
                var importModules = basicBlockDef.ImportModuleRefs?
                    .Select(mRef =>
                    {
                        var importBlocks = mRef.BlockIds?
                            .Select(bId => Definition.GetDefinition(bId))
                            .OfType<BasicBlockDef>().ToArray();
                        var importRequest = new ImportModuleData(mRef.Id, importBlocks, mRef.ModuleName);
                        return importRequest;
                    }).ToArray();
                execControl = CreateBasicControl(blockInstance, basicBlockDef, importModules);
            }
            else if (definition is CompositeBlockDef compositeBlockDef)
                execControl = CreateCompositeControl(blockInstance, compositeBlockDef);
            else throw new NotSupportedException($"Definition of type {definition.GetType().FullName} not supported!");
            execControl.Running += HandleControlRunning;
            execControl.Completed += HandleControlCompleted;
            execControl.Failed += HandleControlFailed;
            return execControl;
        });

    protected virtual IBasicEC CreateBasicControl(BlockInstance blockInstance, BasicBlockDef basicBlockDef, IEnumerable<ImportModuleData> importModules)
        => new BasicEC<TFunctionFramework>(blockInstance, definition: basicBlockDef, importModules, _functionRunner, _blockFrameworkFactory, _functionFrameworkFactory);

    protected virtual ICompositeEC CreateCompositeControl(BlockInstance blockInstance, CompositeBlockDef compositeBlockDef)
        => new CompositeEC<TFunctionFramework>(blockInstance, definition: compositeBlockDef, _blockRunner, _functionRunner, _blockFrameworkFactory, _functionFrameworkFactory, _taskRunner, _taskLimiter);

    protected virtual void HandleControlRunning(object sender, EventArgs ev) => ControlRunning?.Invoke(sender, ev);

    protected virtual void HandleControlCompleted(object sender, EventArgs ev) => ControlCompleted?.Invoke(sender, ev);

    protected virtual void HandleControlFailed(object sender, Exception ex)
    {
        ControlFailed?.Invoke(sender, ex);
        HandleException(ex, sender as IExecutionControl);
    }

    protected async Task TryRunTaskAsync(Func<Task> task)
    {
        var taskScope = _taskLimiter.TryAcquire(count: 1);
        await _taskRunner.RunSyncAsync(taskScope, async (asyncScope) =>
        {
            await using var _ = asyncScope;
            try { await task(); }
            catch (Exception ex) { HandleException(ex); }
        });
    }

    private SimpleScope CreateTaskScope() => new(() => SafeAccessTasks(() =>
    {
        if (_taskCount > 0 && --_taskCount == 0)
        {
            _taskLoopEvent.Set();
            _taskLoopCts.Cancel();
        }
    }));

    private void WaitForTasks(CancellationToken cancellationToken)
    {
        try
        {
            if (!cancellationToken.IsCancellationRequested)
                _taskLoopEvent.Wait(cancellationToken: cancellationToken);
        }
        catch { }
    }

    private void EnqueueTask(Func<IDisposable, Task> task)
    {
        SafeAccessTasks(() =>
        {
            _taskCount++;
            _tasks.Enqueue(() => task(CreateTaskScope()));
            _taskLoopEvent.Set();
        });
    }

    private void SafeAccessTasks(Action action)
    {
        lock (_taskLoopEvent) { action(); }
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        Running -= HandleControlRunning;
        Completed -= HandleControlCompleted;
        Failed -= HandleControlFailed;

        _taskLoopCts?.Dispose();
        _taskLoopEvent?.Dispose();
        foreach (var control in _blockExecControlMap.Values)
            control.Dispose();
        base.Dispose();
    }
}
