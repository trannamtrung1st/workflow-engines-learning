namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionResult
{
    public BlockExecutionResult(IEnumerable<BlockTransitionResult> transitionResults, IEnumerable<string> outputEvents)
    {
        TransitionResults = transitionResults;
        if (transitionResults.Any())
        {
            var first = transitionResults.First();
            var last = transitionResults.Last();
            FromState = first.FromState;
            FinalState = last.ToState;
        }
        OutputEvents = outputEvents;
    }

    public IEnumerable<BlockTransitionResult> TransitionResults { get; }
    public string FromState { get; }
    public string FinalState { get; }
    public IEnumerable<string> OutputEvents { get; }
}
