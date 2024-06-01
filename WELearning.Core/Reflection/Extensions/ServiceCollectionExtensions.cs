using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.Reflection.Abstracts;

namespace WELearning.Core.Reflection.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultTypeProvider(this IServiceCollection services)
    {
        return services.AddTransient<ITypeProvider, TypeProvider>();
    }
}
