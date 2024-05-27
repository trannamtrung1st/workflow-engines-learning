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
    private readonly ManualResetEventSlim _completeWait;
    public BlockExecutionControl(FunctionBlockInstance block)
    {
        _inputBindings = new();
        _outputBindings = new();
        _internalBindings = new();
        _completeWait = new(initialState: true);
        Block = block;
        CurrentState = block.Definition.ExecutionControlChart.InitialState;
    }

    public FunctionBlockInstance Block { get; }
    public virtual string CurrentState { get; protected set; }
    public virtual Exception Exception { get; protected set; }

    private EBlockExecutionStatus _status;
    public virtual EBlockExecutionStatus Status
    {
        get => _status; protected set
        {
            switch (value)
            {
                case EBlockExecutionStatus.Running: _completeWait.Reset(); break;
                default: _completeWait.Set(); break;
            }
            _status = value;
        }
    }

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
        Status = EBlockExecutionStatus.Running;
        try
        {
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
            return new BlockExecutionResult(transitionResults, outputEvents: GetOutputEvents());
        }
        catch (Exception ex)
        {
            Exception = ex;
            Status = EBlockExecutionStatus.Failed;
            throw;
        }
    }

    public void WaitForCompletion(CancellationToken cancellationToken) => _completeWait.Wait(cancellationToken);
}
