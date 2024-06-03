namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(IEnumerable<VariableBinding> bindings, string triggerEvent = null)
    {
        RunId = Guid.NewGuid();
        Bindings = bindings;
        TriggerEvent = triggerEvent;
    }

    public Guid RunId { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public string TriggerEvent { get; }
}