namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionResult
{
    public BlockExecutionResult(IEnumerable<BlockTransitionResult> transitionResults)
    {
        TransitionResults = transitionResults;
        var first = transitionResults.First();
        var last = transitionResults.Last();
        FromState = first.FromState;
        FinalState = last.ToState;
        var outputEvents = new HashSet<string>();
        foreach (var transitionResult in transitionResults)
        {
            foreach (var outputEvent in transitionResult.OutputEvents)
                outputEvents.Add(outputEvent);
        }
        OutputEvents = outputEvents;
    }

    public IEnumerable<BlockTransitionResult> TransitionResults { get; }
    public string FromState { get; }
    public string FinalState { get; }
    public IEnumerable<string> OutputEvents { get; }
}
