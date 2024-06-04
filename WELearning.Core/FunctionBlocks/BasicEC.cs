using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks;

public class BasicEC<TFramework> : BaseEC<TFramework, BasicBlockDef>, IBasicEC, IDisposable where TFramework : IBlockFramework
{
    public BasicEC(
        BlockInstance block,
        BasicBlockDef definition,
        IFunctionRunner<TFramework> functionRunner,
        IBlockFrameworkFactory<TFramework> blockFrameworkFactory) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
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
        var blockFramework = _blockFrameworkFactory.Create(this);
        var globalObject = new BlockGlobalObject<TFramework>(blockFramework);
        var flattenArguments = new List<(string, object)>();
        var flattenOutputs = new List<string>() { BuiltInVariables.EventsOutputVariable };
        flattenArguments.Add((nameof(BlockGlobalObject<TFramework>.FB), blockFramework));
        FlattenInputs(flattenArguments);
        FlattenOutputs(flattenOutputs);

        async Task<bool> Evaluate(Function condition)
        {
            try
            {
                RunningFunction = condition;
                var (result, optimizationScope) = await _functionRunner.Run<bool>(
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
                    actionFunction, globalObject, flattenArguments, flattenOutputs,
                    optimizationScopeId.Value, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
            }
            catch (CompilationError ex) { throw new FunctionCompilationError(ex, actionFunction); }
            catch (RuntimeException ex) { throw new FunctionRuntimeException(ex, actionFunction); }
        }

        var outputEvents = new HashSet<string>();
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

        TryAddEvents(outputEvents, blockFramework.OutputEvents);
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

    private void TryAddEvents(HashSet<string> events, IEnumerable<string> outputEvents)
    {
        if (outputEvents?.Any() != true) return;
        foreach (var ev in outputEvents)
            events.Add(ev);
    }
}
