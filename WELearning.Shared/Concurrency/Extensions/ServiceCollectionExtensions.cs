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
}