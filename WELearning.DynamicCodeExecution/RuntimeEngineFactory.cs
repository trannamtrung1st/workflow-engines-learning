using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution;

public class RuntimeEngineFactory : IRuntimeEngineFactory
{
    private readonly IEnumerable<IRuntimeEngine> _engines;
    public RuntimeEngineFactory(IEnumerable<IRuntimeEngine> engines)
    {
        _engines = engines;
    }

    public IRuntimeEngine CreateEngine(ERuntime runtime)
    {
        var engine = _engines.FirstOrDefault(e => e.CanRun(runtime));
        if (engine == null) throw new NotSupportedException($"Runtime {runtime} not supported");
        return engine;
    }
}
