using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner<TFramework> : IBlockRunner<TFramework> where TFramework : IBlockFramework
{
    private readonly ILogicRunner<TFramework> _logicRunner;

    public BlockRunner(ILogicRunner<TFramework> logicRunner)
    {
        _logicRunner = logicRunner;
    }

    public async Task<BlockExecutionResult> Run(RunBlockRequest request, BlockExecutionControl control, TFramework blockFramework)
    {
        control.Status = EBlockExecutionStatus.Running;
        try
        {
            var block = request.Block;
            var globalObject = ConstructGlobalObject(blockFramework);
            var triggerEvent = request.TriggerEvent ?? block.DefaultTriggerEvent;
            var transitionResults = new List<BlockTransitionResult>();
            var transition = await FindTransition(block.ExecutionControlChart, triggerEvent, control, globalObject);
            if (transition == null) throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");
            do
            {
                var fromState = control.CurrentState;
                var toState = transition.ToState;
                control.CurrentState = transition.ToState;
                if (transition.ActionLogicId != null)
                {
                    var actionLogic = block.Logics.FirstOrDefault(l => l.Id == transition.ActionLogicId);
                    if (actionLogic == null) throw new KeyNotFoundException($"Action logic {transition.ActionLogicId} not found!");
                    await _logicRunner.Run(actionLogic, globalObject);
                }
                transitionResults.Add(new(fromState, toState));
                globalObject = ConstructGlobalObject(blockFramework);
                transition = await FindTransition(block.ExecutionControlChart, BlockStateTransition.DirectTransitionEvent, control, globalObject);
            } while (transition != null);
            control.Status = EBlockExecutionStatus.Completed;
            return new BlockExecutionResult(transitionResults, outputEvents: blockFramework.OutputEvents);
        }
        catch (Exception ex)
        {
            control.Exception = ex;
            control.Status = EBlockExecutionStatus.Failed;
            throw;
        }
    }

    protected virtual async Task<BlockStateTransition> FindTransition(
        BlockExecutionControlChart controlChart,
        string triggerEvent,
        BlockExecutionControl control,
        BlockGlobalObject<TFramework> globalObject)
    {
        foreach (var transition in controlChart.StateTransitions)
        {
            if (transition.FromState == control.CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await ValidCondition(transition.TriggerCondition, globalObject)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    protected virtual async Task<bool> ValidCondition(Logic triggerCondition, BlockGlobalObject<TFramework> globalObject)
    {
        var valid = await _logicRunner.Run<bool>(triggerCondition, globalObject: globalObject);
        return valid;
    }

    protected virtual BlockGlobalObject<TFramework> ConstructGlobalObject(TFramework blockFramework)
        => new(blockFramework);
}