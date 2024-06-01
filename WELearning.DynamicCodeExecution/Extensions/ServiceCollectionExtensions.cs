using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultRuntimeEngineFactory(this IServiceCollection services)
    {
        return services.AddTransient<IRuntimeEngineFactory, RuntimeEngineFactory>();
    }

    public static IServiceCollection AddKeyedLockManager(this IServiceCollection services)
    {
        return services.AddTransient<IKeyedLockManager, KeyedLockManager>();
    }
}
