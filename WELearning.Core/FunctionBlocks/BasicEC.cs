using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks;

public class BasicEC<TFunctionFramework> : BaseEC<BasicBlockDef>, IBasicEC, IDisposable
    where TFunctionFramework : class
{
    private readonly TFunctionFramework _functionFramework;

    public BasicEC(
        BlockInstance block,
        BasicBlockDef definition,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        TFunctionFramework functionFramework) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
        _functionFramework = functionFramework;
        CurrentState = Definition.ExecutionControlChart?.InitialState;
    }

    public virtual Function RunningFunction { get; protected set; }
    public virtual string CurrentState { get; protected set; }
    private BFBExecutionResult _result;
    public override BlockExecutionResult Result
    {
        get => _result;
        protected set => _result = value as BFBExecutionResult;
    }
    BFBExecutionResult IBasicEC.Result => _result;

    public override event EventHandler Running;
    public override event EventHandler<Exception> Failed;
    public override event EventHandler Completed;

    protected virtual async Task<BlockStateTransition> FindTransition(string triggerEvent, Func<Function, Task<bool>> Evaluate)
    {
        foreach (var transition in Definition.ExecutionControlChart.StateTransitions)
        {
            if (transition.FromState == CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await Evaluate(transition.TriggerCondition)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    public override async Task Execute(RunBlockRequest request, Guid? optimizationScopeId)
    {
        EnterOrThrow();
        var triggerEvent = GetTriggerOrDefault(request.TriggerEvent);
        HashSet<IDisposable> optimizationScopes = null;
        try
        {
            IEnumerable<string> outputEvents = Array.Empty<string>();
            var fromState = CurrentState;
            PrepareRunningStatus(request);
            Running?.Invoke(this, EventArgs.Empty);
            PrepareStates(request.Bindings);

            if (Definition.ExecutionControlChart != null)
            {
                optimizationScopes = new HashSet<IDisposable>();
                outputEvents = await TriggerStateMachine(triggerEvent, optimizationScopes, optimizationScopeId);
            }

            var finalState = CurrentState;
            _result = new BFBExecutionResult(fromState, finalState, outputEvents: outputEvents);
            PrepareCompletedStatus();
            Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (SetFailedStatus())
            {
                PrepareFailedStatus(ex, this);
                Failed?.Invoke(this, ex);
            }
            throw;
        }
        finally
        {
            _idleWait.Set();
            if (optimizationScopes != null)
                foreach (var optimizationScope in optimizationScopes)
                    optimizationScope.Dispose();
        }
    }

    protected virtual async Task<IEnumerable<string>> TriggerStateMachine(
        string triggerEvent, HashSet<IDisposable> optimizationScopes, Guid? optimizationScopeId)
    {
        optimizationScopeId ??= Guid.NewGuid();
        var outputEvents = new HashSet<string>();
        Task Publish(string @event) { outputEvents.Add(@event); return Task.CompletedTask; }
        var blockFramework = _blockFrameworkFactory.Create(this);
        var (flattenArguments, flattenOutputs) = PrepareArguments(blockFramework, Publish);
        var globalObject = new BlockGlobalObject<TFunctionFramework>(_functionFramework, blockFramework, Publish);

        async Task<bool> Evaluate(Function condition)
        {
            try
            {
                RunningFunction = condition;
                var (result, optimizationScope) = await _functionRunner.Run<bool, TFunctionFramework>(
                    condition, globalObject: globalObject, flattenArguments, flattenOutputs,
                    optimizationScopeId.Value, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
                return result;
            }
            catch (CompilationError ex) { throw new FunctionCompilationError(ex, condition); }
            catch (RuntimeException ex) { throw new FunctionRuntimeException(ex, condition); }
        }

        async Task RunAction(Function actionFunction)
        {
            try
            {
                RunningFunction = actionFunction;
                var optimizationScope = await _functionRunner.Run(
                    actionFunction, blockFramework, globalObject, flattenArguments, flattenOutputs,
                    optimizationScopeId.Value, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
            }
            catch (CompilationError ex) { throw new FunctionCompilationError(ex, actionFunction); }
            catch (RuntimeException ex) { throw new FunctionRuntimeException(ex, actionFunction); }
        }

        var transition = await FindTransition(triggerEvent, Evaluate);
        if (transition == null) throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");

        do
        {
            CurrentState = transition.ToState;
            if (transition.ActionFunctionIds?.Any() == true)
            {
                foreach (var actionFunctionId in transition.ActionFunctionIds)
                {
                    var actionFunction = Definition.Functions.FirstOrDefault(l => l.Id == actionFunctionId);
                    if (actionFunction == null) throw new KeyNotFoundException($"Action function {actionFunction} not found!");
                    await RunAction(actionFunction);
                }
            }
            TryAddEvents(outputEvents, transition.DefaultOutputEvents);
            transition = await FindTransition(BlockStateTransition.DirectTransitionEvent, Evaluate);
        } while (transition != null);

        return outputEvents;
    }

    protected override void RefreshOutputs()
    {
        var outputEvents = Definition.Events.Where(e => !e.IsInput && _result.OutputEvents.Contains(e.Name));
        foreach (var outputEvent in outputEvents)
        {
            foreach (var variableName in outputEvent.VariableNames)
            {
                var valueObject = GetOutput(variableName);
                valueObject.TryCommit();
            }
        }
    }

    private static void TryAddEvents(HashSet<string> events, IEnumerable<string> outputEvents)
    {
        if (outputEvents?.Any() != true) return;
        foreach (var ev in outputEvents)
            events.Add(ev);
    }

    protected virtual (List<(string, object)> FlattenArguments, List<string> FlattenOutputs) PrepareArguments(
        IBlockFramework blockFramework, Func<string, Task> Publish)
    {
        var flattenVars = new HashSet<string>();
        var flattenArguments = new List<(string, object)>
        {
            (BuiltInVariables.FB, _functionFramework),
            (BuiltInVariables.Publish, Publish),
            (BuiltInVariables.IN, blockFramework.InputBindings),
            (BuiltInVariables.OUT, blockFramework.OutputBindings),
            (BuiltInVariables.INOUT, blockFramework.InOutBindings),
            (BuiltInVariables.INTERNAL, blockFramework.InternalBindings),
        };
        var flattenOutputs = new List<string>();
        var variables = Definition.Variables;

        foreach (var variable in variables)
        {
            if (!flattenVars.Add(variable.Name)) continue;
            var valueObject = GetValueObject(variable.Name, variable.VariableType);
            var refBinding = blockFramework.GetBindingFor(valueObject);

            if (variable.CanOutput())
                flattenOutputs.Add(variable.Name);

            switch (variable.DataType)
            {
                case Core.Constants.EDataType.Reference:
                    flattenArguments.Add((variable.Name, refBinding));
                    break;
                default:
                    flattenArguments.Add((variable.Name, valueObject.Value));
                    break;
            }
        }
        return (flattenArguments, flattenOutputs);
    }

}
