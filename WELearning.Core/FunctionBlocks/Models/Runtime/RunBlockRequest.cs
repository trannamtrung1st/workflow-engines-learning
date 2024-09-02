using WELearning.DynamicCodeExecution;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunBlockRequest
{
    public RunBlockRequest(
        Guid runId, IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null,
        IReadOnlyDictionary<string, object> reservedInputs = null,
        IDictionary<string, IOptimizationScope> optimizationScopes = null)
    {
        RunId = runId;
        Bindings = bindings;
        TriggerEvent = triggerEvent;
        Tokens = tokens;
        ReservedInputs = reservedInputs;
        Tracker = new CodeExecutionTracker();
        OptimizationScopes = optimizationScopes;
    }

    public RunBlockRequest(
        IEnumerable<VariableBinding> bindings, RunTokens tokens, string triggerEvent = null,
        IReadOnlyDictionary<string, object> reservedInputs = null,
        IDictionary<string, IOptimizationScope> optimizationScopes = null)
        : this(runId: Guid.NewGuid(), bindings, tokens, triggerEvent, reservedInputs, optimizationScopes)
    {
    }

    public Guid RunId { get; }
    public IEnumerable<VariableBinding> Bindings { get; }
    public IReadOnlyDictionary<string, object> ReservedInputs { get; }
    public string TriggerEvent { get; }
    public RunTokens Tokens { get; }
    public CodeExecutionTracker Tracker { get; }
    public IDictionary<string, IOptimizationScope> OptimizationScopes { get; }
}
