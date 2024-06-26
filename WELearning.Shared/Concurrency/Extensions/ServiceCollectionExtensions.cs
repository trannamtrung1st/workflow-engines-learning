using Microsoft.Extensions.DependencyInjection;
using WELearning.Shared.Concurrency;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryLockManager(this IServiceCollection services)
    {
        return services.AddSingleton<IInMemoryLockManager, InMemoryLockManager>();
    }

    public static IServiceCollection AddDefaultDistributedLockManager(this IServiceCollection services)
    {
        return services.AddSingleton<IDistributedLockManager, InMemoryLockManager>();
    }

    public static IServiceCollection AddDefaultSyncAsyncTaskRunner(this IServiceCollection services, Action<TaskLimiterOptions> configure)
    {
        return services.AddSingleton<ISyncAsyncTaskRunner, SyncAsyncTaskRunner>()
            .AddSingleton<ISyncAsyncTaskLimiter, SyncAsyncTaskLimiter>()
            .Configure(configure);
    }

    public static IServiceCollection AddResourceBasedFuzzyRateScaler(this IServiceCollection services)
    {
        return services.AddSingleton<IResourceBasedFuzzyRateScaler, ResourceBasedFuzzyRateScaler>();
    }

    public static IServiceCollection AddResourceBasedRateScaling(this IServiceCollection services, Action<ResourceBasedRateScalingOptions> configure)
    {
        return services.AddSingleton<IRateScalingController, ResourceBasedRateScalingController>()
            .Configure(configure);
    }
}
