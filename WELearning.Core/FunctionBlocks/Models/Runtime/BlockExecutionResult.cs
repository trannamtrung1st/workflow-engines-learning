namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionResult
{
    public BlockExecutionResult(IEnumerable<string> outputEvents)
    {
        OutputEvents = outputEvents;
    }

    public IEnumerable<string> OutputEvents { get; }
}
