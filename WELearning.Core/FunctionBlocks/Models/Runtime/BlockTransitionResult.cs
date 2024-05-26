using System.Collections.Immutable;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockTransitionResult
{
    public BlockTransitionResult(string fromState, string toState, IImmutableSet<string> outputEvents)
    {
        FromState = fromState;
        ToState = toState;
        OutputEvents = outputEvents;
    }

    public string FromState { get; }
    public string ToState { get; }
    public IImmutableSet<string> OutputEvents { get; }
}