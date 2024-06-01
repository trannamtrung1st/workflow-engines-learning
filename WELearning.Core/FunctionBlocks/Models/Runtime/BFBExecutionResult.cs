namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BFBExecutionResult : BlockExecutionResult
{
    public BFBExecutionResult(string fromState, string finalState, IEnumerable<string> outputEvents) : base(outputEvents)
    {
        FromState = fromState;
        FinalState = finalState;
    }

    public string FromState { get; }
    public string FinalState { get; }
}