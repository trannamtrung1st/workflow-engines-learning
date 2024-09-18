using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Exceptions;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks;

public class BasicEC<TFunctionFramework> : BaseEC<BasicBlockDef>, IBasicEC, IDisposable
    where TFunctionFramework : IFunctionFramework
{
    private readonly IBlockFramework _blockFramework;
    private readonly TFunctionFramework _functionFramework;
    private readonly IEnumerable<ImportModule> _importModules;

    public BasicEC(
        BlockInstance block,
        BasicBlockDef definition,
        ImportBlocksRequest importBlocksRequest,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        IFunctionFrameworkFactory<TFunctionFramework> functionFrameworkFactory) : base(block, definition, functionRunner, blockFrameworkFactory)
    {
        _blockFramework = _blockFrameworkFactory.Create(this);
        _functionFramework = functionFrameworkFactory.Create(_blockFramework);
        CurrentState = Definition.ExecutionControlChart?.InitialState;
        _importModules = PrepareModules(importBlocksRequest);
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
    public IFunctionFramework FunctionFramework => _functionFramework;

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

    public override async Task Execute(RunBlockRequest request, string optimizationScopeId)
    {
        EnterOrThrow();
        var optimizationScopes = request.OptimizationScopes ?? new Dictionary<string, IOptimizationScope>();
        try
        {
            IEnumerable<string> outputEvents = Array.Empty<string>();
            var fromState = CurrentState;
            HandleRunning(request);

            if (Definition.ExecutionControlChart != null)
            {
                var triggerEvent = GetTriggerOrDefault(request.TriggerEvent);
                outputEvents = await TriggerStateMachine(
                    triggerEvent, reservedInputs: request.ReservedInputs,
                    optimizationScopes, optimizationScopeId);
            }

            var finalState = CurrentState;
            _result = new BFBExecutionResult(fromState, finalState, outputEvents: outputEvents);
            HandleCompleted();
        }
        catch (Exception ex)
        {
            HandleException(ex, this);
            throw;
        }
        finally
        {
            SetIdle();
            if (request.OptimizationScopes is null)
                foreach (var optimizationScope in optimizationScopes.Values)
                    optimizationScope.Dispose();
        }
    }

    protected virtual async Task<IEnumerable<string>> TriggerStateMachine(
        string triggerEvent, IReadOnlyDictionary<string, object> reservedInputs,
        IDictionary<string, IOptimizationScope> optimizationScopes, string optimizationScopeId)
    {
        optimizationScopeId ??= Guid.NewGuid().ToString();
        var outputEvents = new HashSet<string>();
        var publisher = _blockFramework.CreateEventPublisher(outputEvents);
        var (inputs, outputs) = PrepareArguments(_blockFramework, reservedInputs);
        var globalObject = new BlockGlobalObject<TFunctionFramework>(_functionFramework, publisher, reservedInputs);

        async Task<bool> Evaluate(Function condition)
        {
            try
            {
                RunningFunction = condition;
                var (result, optimizationScope) = await _functionRunner.Run<bool, TFunctionFramework>(
                    condition, tracker: CurrentRunRequest.Tracker, globalObject: globalObject, inputs, outputs,
                    modules: _importModules, optimizationScopeId, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null)
                    optimizationScopes[optimizationScope.Id] = optimizationScope;
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
                    actionFunction, tracker: CurrentRunRequest.Tracker, _blockFramework, globalObject, inputs, outputs,
                    modules: _importModules, optimizationScopeId, tokens: CurrentRunRequest.Tokens);
                RunningFunction = null;
                if (optimizationScope != null)
                    optimizationScopes[optimizationScope.Id] = optimizationScope;
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
        if (_result?.OutputEvents.Any() != true)
            return;

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

    protected IEnumerable<ImportModule> PrepareModules(ImportBlocksRequest importBlocksRequest)
    {
        if (importBlocksRequest?.ImportBlocks == null) return null;
        var functions = importBlocksRequest.ImportBlocks.SelectMany(b => b.GetModuleFunctions()).ToArray();
        var module = new ImportModule(id: Definition.Id, moduleName: importBlocksRequest.ModuleName, functions);
        return new[] { module };
    }

    protected virtual (Dictionary<string, object> Inputs, Dictionary<string, object> Outputs) PrepareArguments(IBlockFramework blockFramework, IReadOnlyDictionary<string, object> reservedInputs)
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

        reservedInputs?.AssignTo(inputs);
        _functionFramework.GetReservedInputs()?.AssignTo(inputs);
        return (inputs, outputs);
    }
}
