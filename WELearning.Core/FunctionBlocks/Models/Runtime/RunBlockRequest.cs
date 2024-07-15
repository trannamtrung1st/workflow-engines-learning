using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(Guid runId, IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null, IReadOnlyDictionary<string, object> reservedInputs = null)
    {
        RunId = runId;
        Bindings = bindings;
        TriggerEvent = triggerEvent;
        Tokens = tokens;
        ReservedInputs = reservedInputs;
    }

    public RunBlockRequest(IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null, IReadOnlyDictionary<string, object> reservedInputs = null)
        : this(runId: Guid.NewGuid(), bindings, tokens, triggerEvent, reservedInputs)
    {
    }

    public Guid RunId { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public IReadOnlyDictionary<string, object> ReservedInputs { get; }
    public string TriggerEvent { get; }
    public RunTokens Tokens { get; }
}
