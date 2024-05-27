using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockExecutionControl : IBlockExecutionControl
{
    private readonly ConcurrentDictionary<string, ValueObject> _inputSnapshot;
    private readonly ConcurrentDictionary<string, ValueObject> _outputSnapshot;
    private readonly ConcurrentDictionary<string, ValueObject> _internalDataSnapshot;
    public BlockExecutionControl(FunctionBlockInstance block)
    {
        _inputSnapshot = new();
        _outputSnapshot = new();
        _internalDataSnapshot = new();
        Block = block;
        CurrentState = block.Definition.ExecutionControlChart.InitialState;
    }

    public FunctionBlockInstance Block { get; }
    public virtual string CurrentState { get; protected set; }
    public virtual Exception Exception { get; protected set; }
    public virtual EBlockExecutionStatus Status { get; protected set; }

    public virtual ValueObject GetInput(string key) => GetValueObject(_inputSnapshot, key);
    public virtual ValueObject GetOutput(string key) => GetValueObject(_outputSnapshot, key);
    public virtual ValueObject GetInternalData(string key) => GetValueObject(_internalDataSnapshot, key);
    private ValueObject GetValueObject(ConcurrentDictionary<string, ValueObject> source, string key)
        => source.GetOrAdd(key, (key) => new ValueObject());

    public virtual async Task<BlockStateTransition> FindTransition(
        string triggerEvent,
        Func<Logic, Task<bool>> EvaluateCondition)
    {
        foreach (var transition in Block.Definition.ExecutionControlChart.StateTransitions)
        {
            if (transition.FromState == CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await EvaluateCondition(transition.TriggerCondition)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    public virtual async Task<BlockExecutionResult> Execute(string triggerEvent,
        Func<Logic, Task<bool>> EvaluateCondition,
        Func<Logic, Task> RunAction,
        Func<IEnumerable<string>> GetOutputEvents)
    {
        Status = EBlockExecutionStatus.Running;
        try
        {
            triggerEvent = triggerEvent ?? Block.Definition.DefaultTriggerEvent;
            var transitionResults = new List<BlockTransitionResult>();
            var transition = await FindTransition(triggerEvent, EvaluateCondition);
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
                    await RunAction(actionLogic);
                }
                transitionResults.Add(new(fromState, toState));
                transition = await FindTransition(BlockStateTransition.DirectTransitionEvent, EvaluateCondition);
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
}
