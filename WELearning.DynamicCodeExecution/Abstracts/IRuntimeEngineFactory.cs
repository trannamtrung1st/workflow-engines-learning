using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngineFactory
{
    IEnumerable<ERuntime> SupportedRuntimes { get; }
    IRuntimeEngine CreateEngine(ERuntime runtime);
}
