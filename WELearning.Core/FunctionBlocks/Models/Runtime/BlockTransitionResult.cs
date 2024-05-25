namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockTransitionResult
{
    public BlockTransitionResult(string fromState, string toState, Dictionary<string, object> outputs, IEnumerable<string> outputEvents)
    {
        FromState = fromState;
        ToState = toState;
        Outputs = outputs;
        OutputEvents = outputEvents;
    }

    public string FromState { get; }
    public string ToState { get; }
    public Dictionary<string, object> Outputs { get; }
    public IEnumerable<string> OutputEvents { get; }
}