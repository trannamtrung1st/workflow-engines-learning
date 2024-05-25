using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(FunctionBlock block, string triggerEvent)
    {
        Block = block;
        TriggerEvent = triggerEvent;
    }

    public FunctionBlock Block { get; }
    public string TriggerEvent { get; }
}