using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency.Extensions;

public static class ServiceCollectionExtensions
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

    public static IServiceCollection AddDynamicRateLimiter(this IServiceCollection services, int initialLimit)
    {
        return services.AddTransient<IDynamicRateLimiter>(_ =>
        {
            var limiter = new DynamicRateLimiter();
            limiter.SetLimit(initialLimit);
            return limiter;
        });
    }
}
