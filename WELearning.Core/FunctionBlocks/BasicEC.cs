using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks;

public class BasicEC<TFunctionFramework> : BaseEC<BasicBlockDef>, IBasicEC, IDisposable
    where TFunctionFramework : class
{
    private readonly TFunctionFramework _functionFramework;
    private readonly IEnumerable<ImportModule> _importModules;

    public BasicEC(
        BlockInstance block,
        BasicBlockDef definition,
        IEnumerable<BasicBlockDef> importBlocks,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        TFunctionFramework functionFramework) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
        _functionFramework = functionFramework;
        CurrentState = Definition.ExecutionControlChart?.InitialState;
        _importModules = PrepareModules(importBlocks);
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
        optimizationScopeId ??= Guid.NewGuid();
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
                outputEvents = await TriggerStateMachine(triggerEvent, optimizationScopes, optimizationScopeId.Value);
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
        string triggerEvent, HashSet<IDisposable> optimizationScopes, Guid optimizationScopeId)
    {
        var outputEvents = new HashSet<string>();
        var blockFramework = _blockFrameworkFactory.Create(this);
        var publisher = blockFramework.CreateEventPublisher(outputEvents);
        var (inputs, outputs) = PrepareArguments(blockFramework);
        var globalObject = new BlockGlobalObject<TFunctionFramework>(_functionFramework, blockFramework, publisher);

        async Task<bool> Evaluate(Function condition)
        {
            try
            {
                RunningFunction = condition;
                var (result, optimizationScope) = await _functionRunner.Run<bool, TFunctionFramework>(
                    condition, globalObject: globalObject, inputs, outputs,
                    modules: _importModules, optimizationScopeId, tokens: CurrentRunRequest.Tokens);
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
                    actionFunction, blockFramework, globalObject, inputs, outputs,
                    modules: _importModules, optimizationScopeId, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null)
                    optimizationScopes.Add(optimizationScope);
            }
            catch (CompilationError ex) { throw new FunctionCompilationError(ex, actionFunction); }
            catch (RuntimeException ex) { throw new FunctionRuntimeException(ex, actionFunction); }
        }

        var transition = await FindTransition(triggerEvent, Evaluate)
            ?? throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");

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

    protected IEnumerable<ImportModule> PrepareModules(IEnumerable<BasicBlockDef> importBlocks)
    {
        if (importBlocks == null) return null;
        var functions = importBlocks.SelectMany(b => b.GetModuleFunctions()).ToArray();
        var module = new ImportModule(id: Definition.Id, moduleName: FunctionDefaults.ModuleFunctions, functions);
        return new[] { module };
    }

    protected virtual (Dictionary<string, object> Inputs, Dictionary<string, object> Outputs) PrepareArguments(IBlockFramework blockFramework)
    {
        var inputs = new Dictionary<string, object>();
        var outputs = new Dictionary<string, object>();
        var variables = Definition.Variables;

        foreach (var variable in variables)
        {
            var valueObject = GetValueObject(variable.Name, variable.VariableType);
            var refBinding = blockFramework.GetBindingFor(valueObject);
            Dictionary<string, object> source = variable.CanOutput() ? outputs : inputs;
            switch (variable.DataType)
            {
                case Core.Constants.EDataType.Reference:
                    source[variable.Name] = refBinding;
                    break;
                default:
                    source[variable.Name] = valueObject.Value;
                    break;
            }
        }

        return (inputs, outputs);
    }
}
