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

    public static IServiceCollection AddFuzzyThreadController(this IServiceCollection services)
    {
        return services.AddSingleton<IFuzzyThreadController, FuzzyThreadController>();
    }

    public static IServiceCollection AddResourceBasedConcurrencyScaling(this IServiceCollection services, Action<ResourceBasedConcurrencyScalingOptions> configure)
    {
        return services.AddSingleton<IConcurrencyScalingController, ResourceBasedConcurrencyScalingController>()
            .Configure(configure);
    }
}
