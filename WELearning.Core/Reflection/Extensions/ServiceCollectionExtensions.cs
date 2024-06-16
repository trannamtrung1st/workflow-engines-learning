using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.Reflection;
using WELearning.Core.Reflection.Abstracts;

namespace WELearning.Core.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultTypeProvider(this IServiceCollection services)
    {
        return services.AddSingleton<ITypeProvider, TypeProvider>();
    }
}
