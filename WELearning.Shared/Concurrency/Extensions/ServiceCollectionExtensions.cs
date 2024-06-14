using System;
using Microsoft.Extensions.DependencyInjection;
using WELearning.Shared.Concurrency.Abstracts;

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

    public static IServiceCollection AddDefaultSyncAsyncTaskRunner(this IServiceCollection services, int initialLimit)
    {
        return services.AddSingleton<ISyncAsyncTaskRunner, SyncAsyncTaskRunner>()
            .AddSingleton<ISyncAsyncTaskLimiter>(provider =>
            {
                var limiter = new SyncAsyncTaskLimiter();
                limiter.SetLimit(initialLimit);
                return limiter;
            });
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
