using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(FunctionBlockInstance block, string triggerEvent)
    {
        Block = block;
        TriggerEvent = triggerEvent;
    }

    public FunctionBlockInstance Block { get; }
    public string TriggerEvent { get; }
}