using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockExecutionControl<TFramework> : IBlockExecutionControl, IDisposable where TFramework : IBlockFramework
{
    private readonly ConcurrentDictionary<Variable, ValueObject> _valueMap;
    private readonly SemaphoreSlim _mutexLock;
    private readonly ManualResetEventSlim _idleWait;
    private readonly ILogicRunner<TFramework> _logicRunner;
    private readonly IBlockFrameworkFactory<TFramework> _blockFrameworkFactory;
    public BlockExecutionControl(FunctionBlockInstance block, ILogicRunner<TFramework> logicRunner, IBlockFrameworkFactory<TFramework> blockFrameworkFactory)
    {
        _logicRunner = logicRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _valueMap = new();
        _idleWait = new(initialState: true);
        _mutexLock = new(1);
        Block = block;
        CurrentState = block.Definition.ExecutionControlChart?.InitialState;
    }

    public FunctionBlockInstance Block { get; }
    public virtual string CurrentState { get; protected set; }
    public virtual Exception Exception { get; protected set; }
    public virtual bool IsIdle => _idleWait.IsSet;
    public virtual EBlockExecutionStatus Status { get; protected set; }

    public event EventHandler Running;
    public event EventHandler<Exception> Failed;
    public event EventHandler<BlockExecutionResult> Completed;

    public virtual ValueObject GetInOut(string key) => GetValueObject(key, EBindingType.InOut);
    public virtual ValueObject GetInput(string key) => GetValueObject(key, EBindingType.Input);
    public virtual ValueObject GetOutput(string key) => GetValueObject(key, EBindingType.Output);
    public virtual ValueObject GetInternalData(string key) => GetValueObject(key, EBindingType.Internal);
    public virtual ValueObject GetValueObject(string name, EBindingType type)
    {
        var variable = ValidateBinding(name, type);
        return _valueMap.GetOrAdd(variable, (variable) => new ValueObject(variable));
    }

    public virtual async Task<BlockStateTransition> FindTransition(
        string triggerEvent, Func<Logic, CancellationToken, Task<bool>> Evaluate, CancellationToken cancellationToken)
    {
        foreach (var transition in Block.Definition.ExecutionControlChart.StateTransitions)
        {
            if (transition.FromState == CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await Evaluate(transition.TriggerCondition, cancellationToken)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    public virtual async Task<BlockExecutionResult> Execute(string triggerEvent,
        IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        lock (_idleWait)
        {
            if (!IsIdle) throw new InvalidOperationException("Not ready for execute!");
            _idleWait.Reset();
        }
        var optimizationScopes = new HashSet<IDisposable>();
        try
        {
            Status = EBlockExecutionStatus.Running;
            Running?.Invoke(this, EventArgs.Empty);
            optimizationScopeId ??= Guid.NewGuid();
            var blockFramework = _blockFrameworkFactory.Create(this);
            var globalObject = new BlockGlobalObject<TFramework>(blockFramework);

            async Task<bool> Evaluate(Logic condition, CancellationToken cancellationToken)
            {
                var (result, optimizationScope) = await _logicRunner.Run<bool>(condition, globalObject: globalObject, optimizationScopeId.Value, cancellationToken);
                if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
                return result;
            }

            async Task RunAction(Logic actionLogic, CancellationToken cancellationToken)
            {
                var optimizationScope = await _logicRunner.Run(actionLogic, globalObject, optimizationScopeId.Value, cancellationToken);
                if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
            }

            PrepareStates(bindings);
            triggerEvent ??= Block.Definition.DefaultTriggerEvent;
            var transitionResults = new List<BlockTransitionResult>();
            var transition = await FindTransition(triggerEvent, Evaluate, cancellationToken);
            if (transition == null) throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");
            do
            {
                var fromState = CurrentState;
                var toState = transition.ToState;
                CurrentState = transition.ToState;
                if (transition.ActionLogicIds?.Any() == true)
                {
                    var tasks = new List<Task>();
                    foreach (var actionLogicId in transition.ActionLogicIds)
                    {
                        var actionLogic = Block.Definition.Logics.FirstOrDefault(l => l.Id == actionLogicId);
                        if (actionLogic == null) throw new KeyNotFoundException($"Action logic {actionLogic} not found!");
                        tasks.Add(RunAction(actionLogic, cancellationToken));
                    }
                    await Task.WhenAll(tasks);
                }
                transitionResults.Add(new(fromState, toState));
                transition = await FindTransition(BlockStateTransition.DirectTransitionEvent, Evaluate, cancellationToken);
            } while (transition != null);
            Status = EBlockExecutionStatus.Completed;
            var executionResult = new BlockExecutionResult(transitionResults, outputEvents: blockFramework.OutputEvents);
            Completed?.Invoke(this, executionResult);
            return executionResult;
        }
        catch (Exception ex)
        {
            Exception = ex;
            Status = EBlockExecutionStatus.Failed;
            Failed?.Invoke(this, ex);
            throw;
        }
        finally
        {
            _idleWait.Set();
            foreach (var optimizationScope in optimizationScopes)
                optimizationScope.Dispose();
        }
    }

    public virtual void WaitForIdle(CancellationToken cancellationToken) => _idleWait.Wait(cancellationToken);

    public virtual async Task MutexAccess(Func<Task> Task, CancellationToken cancellationToken)
    {
        await _mutexLock.WaitAsync(cancellationToken);
        try { await Task(); }
        finally { _mutexLock.Release(); }
    }

    protected virtual void PrepareStates(IEnumerable<VariableBinding> bindings)
    {
        foreach (var binding in bindings)
        {
            var valueObject = GetValueObject(binding.VariableName, binding.Type);
            valueObject.Value = binding.Value;
        }
    }

    private Variable ValidateBinding(string name, EBindingType type)
    {
        var isInOrOut = type == EBindingType.Input || type == EBindingType.Output || type == EBindingType.InOut;
        var variable = Block.Definition.Variables.FirstOrDefault(v => v.Name == name
            && (
                v.BindingType == type
                || (v.BindingType == EBindingType.InOut && isInOrOut)
            ));
        return variable ?? throw new KeyNotFoundException(name);
    }

    public void Dispose()
    {
        _mutexLock.Dispose();
        _idleWait.Dispose();
    }
}
