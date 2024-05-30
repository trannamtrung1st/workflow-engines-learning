using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockExecutionControl : IBlockExecutionControl
{
    private readonly ConcurrentDictionary<string, VariableBinding> _inputBindings;
    private readonly ConcurrentDictionary<string, VariableBinding> _outputBindings;
    private readonly ConcurrentDictionary<string, VariableBinding> _internalBindings;
    private readonly ManualResetEventSlim _idleWait;
    public BlockExecutionControl(FunctionBlockInstance block)
    {
        _inputBindings = new();
        _outputBindings = new();
        _internalBindings = new();
        _idleWait = new(initialState: true);
        Block = block;
        CurrentState = block.Definition.ExecutionControlChart.InitialState;
    }

    public FunctionBlockInstance Block { get; }
    public virtual string CurrentState { get; protected set; }
    public virtual Exception Exception { get; protected set; }
    public virtual bool IsRunning => Status == EBlockExecutionStatus.Running;
    public virtual EBlockExecutionStatus Status { get; protected set; }

    public event EventHandler Running;
    public event EventHandler<Exception> Failed;
    public event EventHandler<BlockExecutionResult> Completed;

    public virtual VariableBinding GetInput(string key) => GetVariableBinding(_inputBindings, key);
    public virtual VariableBinding GetOutput(string key) => GetVariableBinding(_outputBindings, key);
    public virtual VariableBinding GetInternalData(string key) => GetVariableBinding(_internalBindings, key);
    private VariableBinding GetVariableBinding(ConcurrentDictionary<string, VariableBinding> source, string name)
        => source.GetOrAdd(name, (name) => new VariableBinding(name));

    public virtual async Task<BlockStateTransition> FindTransition(
        string triggerEvent, Func<Logic, CancellationToken, Task<bool>> EvaluateCondition,
        CancellationToken cancellationToken)
    {
        foreach (var transition in Block.Definition.ExecutionControlChart.StateTransitions)
        {
            if (transition.FromState == CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await EvaluateCondition(transition.TriggerCondition, cancellationToken)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    public virtual async Task<BlockExecutionResult> Execute(string triggerEvent,
        Func<Logic, CancellationToken, Task<bool>> EvaluateCondition,
        Func<Logic, CancellationToken, Task> RunAction,
        Func<IEnumerable<string>> GetOutputEvents,
        CancellationToken cancellationToken)
    {
        WaitForCompletion(cancellationToken);
        _idleWait.Reset();
        try
        {
            Status = EBlockExecutionStatus.Running;
            Running?.Invoke(this, EventArgs.Empty);
            triggerEvent = triggerEvent ?? Block.Definition.DefaultTriggerEvent;
            var transitionResults = new List<BlockTransitionResult>();
            var transition = await FindTransition(triggerEvent, EvaluateCondition, cancellationToken);
            if (transition == null) throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");
            do
            {
                var fromState = CurrentState;
                var toState = transition.ToState;
                CurrentState = transition.ToState;
                if (transition.ActionLogicId != null)
                {
                    var actionLogic = Block.Definition.Logics.FirstOrDefault(l => l.Id == transition.ActionLogicId);
                    if (actionLogic == null) throw new KeyNotFoundException($"Action logic {transition.ActionLogicId} not found!");
                    await RunAction(actionLogic, cancellationToken);
                }
                transitionResults.Add(new(fromState, toState));
                transition = await FindTransition(BlockStateTransition.DirectTransitionEvent, EvaluateCondition, cancellationToken);
            } while (transition != null);
            Status = EBlockExecutionStatus.Completed;
            var executionResult = new BlockExecutionResult(transitionResults, outputEvents: GetOutputEvents());
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
        finally { _idleWait.Set(); }
    }

    public void WaitForCompletion(CancellationToken cancellationToken) => _idleWait.Wait(cancellationToken);
}
