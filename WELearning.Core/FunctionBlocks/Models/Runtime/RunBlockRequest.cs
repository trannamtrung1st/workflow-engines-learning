using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(Guid runId, IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null)
    {
        RunId = runId;
        Bindings = bindings;
        TriggerEvent = triggerEvent;
        Tokens = tokens;
    }

    public RunBlockRequest(IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null)
        : this(runId: Guid.NewGuid(), bindings, tokens, triggerEvent)
    {
    }

    public Guid RunId { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public string TriggerEvent { get; }
    public RunTokens Tokens { get; }
}
