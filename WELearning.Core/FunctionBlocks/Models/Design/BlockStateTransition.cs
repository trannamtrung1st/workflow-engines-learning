namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockStateTransition
{
    public const string DirectTransitionEvent = "1";

    public BlockStateTransition(string fromState, string toState, string triggerEventName = DirectTransitionEvent)
    {
        FromState = fromState;
        ToState = toState;
        TriggerEventName = triggerEventName;
    }

    public string TriggerEventName { get; set; }
    public string FromState { get; set; }
    public string ToState { get; set; }
    public Logic TriggerCondition { get; set; }
    public IEnumerable<string> ActionLogicIds { get; set; }
}