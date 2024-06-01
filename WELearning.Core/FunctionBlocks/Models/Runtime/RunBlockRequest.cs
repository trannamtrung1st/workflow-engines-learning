namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(IEnumerable<VariableBinding> bindings, string triggerEvent = null)
    {
        Bindings = bindings;
        TriggerEvent = triggerEvent;
    }

    public IEnumerable<VariableBinding> Bindings { get; }
    public string TriggerEvent { get; }
}