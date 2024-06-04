using Microsoft.Extensions.DependencyInjection;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryLockManager(this IServiceCollection services)
    {
        return services.AddSingleton<ILockManager, InMemoryLockManager>();
    }
}
