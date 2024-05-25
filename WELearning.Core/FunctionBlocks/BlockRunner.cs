using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner : IBlockRunner
{
    private readonly ILogicRunner _logicRunner;
    public BlockRunner(ILogicRunner logicRunner)
    {
        _logicRunner = logicRunner;
    }

    public async Task<BlockExecutionResult> Run(RunBlockRequest request, BlockExecutionControl control)
    {
        control.Status = EBlockExecutionStatus.Running;
        try
        {
            var block = request.Block;
            var logicArguments = PrepareLogicArguments(control);
            var triggerEvent = request.TriggerEvent ?? block.DefaultTriggerEvent;
            var transitionResults = new List<BlockTransitionResult>();
            var transition = await FindTransition(block.ExecutionControlChart, triggerEvent, control, logicArguments);
            if (transition == null) throw new KeyNotFoundException($"Transition for event {triggerEvent} not found!");
            do
            {
                control.CurrentState = transition.ToState;
                var actionLogic = block.Logics.FirstOrDefault(l => l.Id == transition.ActionLogicId);
                var transitionResult = await _logicRunner.Run<BlockTransitionResult>(actionLogic, arguments: logicArguments);
                if (transitionResult == null) throw new InvalidOperationException("Block logic should return a transition result");
                transitionResults.Add(transitionResult);
                transition = await FindTransition(block.ExecutionControlChart, BlockStateTransition.DirectTransitionEvent, control, logicArguments);
            } while (transition != null);
            control.Status = EBlockExecutionStatus.Completed;
        }
        catch (Exception ex)
        {
            control.Exception = ex;
            control.Status = EBlockExecutionStatus.Failed;
            throw;
        }
        return default;
    }

    protected virtual async Task<BlockStateTransition> FindTransition(
        BlockExecutionControlChart controlChart,
        string triggerEvent,
        BlockExecutionControl control,
        object logicArguments)
    {
        foreach (var transition in controlChart.StateTransitions)
        {
            if (transition.FromState == control.CurrentState
                && transition.TriggerEventName == triggerEvent
                && (
                    transition.TriggerCondition == null
                    || await ValidCondition(transition.TriggerCondition, logicArguments)
                ))
            {
                return transition;
            }
        }
        return null;
    }

    protected virtual async Task<bool> ValidCondition(Logic triggerCondition, object logicArguments)
    {
        var valid = await _logicRunner.Run<bool>(triggerCondition, arguments: logicArguments);
        return valid;
    }

    protected virtual object PrepareLogicArguments(BlockExecutionControl control)
    {
        return control.InputSnapshot;
    }
}