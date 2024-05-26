using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Engines;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultRuntimeEngines(this IServiceCollection services)
    {
        return services.AddTransient<IRuntimeEngine, CSharpScriptEngine>()
            .AddTransient<IRuntimeEngine, CSharpCompiledEngine>();
    }

    public static IServiceCollection AddDefaultRuntimeEngineFactory(this IServiceCollection services)
    {
        return services.AddTransient<IRuntimeEngineFactory, RuntimeEngineFactory>();
    }
}
