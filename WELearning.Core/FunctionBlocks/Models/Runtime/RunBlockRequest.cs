namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(Guid runId, IEnumerable<VariableBinding> bindings, string triggerEvent = null)
    {
        RunId = runId;
        Bindings = bindings;
        TriggerEvent = triggerEvent;
    }

    public RunBlockRequest(IEnumerable<VariableBinding> bindings, string triggerEvent = null)
        : this(runId: Guid.NewGuid(), bindings, triggerEvent)
    {
    }

    public Guid RunId { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public string TriggerEvent { get; }
}