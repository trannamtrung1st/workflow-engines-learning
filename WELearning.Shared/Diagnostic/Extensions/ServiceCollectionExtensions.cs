using Microsoft.Extensions.DependencyInjection;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Shared.Diagnostic.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceMonitor(this IServiceCollection services)
    {
        return services.AddSingleton<IResourceMonitor, ResourceMonitor>();
    }

    public static IServiceCollection AddRateMonitor(this IServiceCollection services)
    {
        return services.AddSingleton<IRateMonitor, RateMonitor>();
    }
}
