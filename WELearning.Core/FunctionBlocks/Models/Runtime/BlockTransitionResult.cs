namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockTransitionResult
{
    public BlockTransitionResult(string fromState, string toState)
    {
        FromState = fromState;
        ToState = toState;
    }

    public string FromState { get; }
    public string ToState { get; }
}