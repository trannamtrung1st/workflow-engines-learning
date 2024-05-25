using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngineFactory
{
    IRuntimeEngine CreateEngine(ERuntime runtime);
}
