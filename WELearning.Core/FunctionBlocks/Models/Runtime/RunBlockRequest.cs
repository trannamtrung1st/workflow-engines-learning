using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(FunctionBlockInstance block, IEnumerable<VariableBinding> bindings, string triggerEvent = null)
    {
        Block = block;
        Bindings = bindings;
        TriggerEvent = triggerEvent;
    }

    public FunctionBlockInstance Block { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public string TriggerEvent { get; }
}