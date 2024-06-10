using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultBlockRunner(this IServiceCollection services)
    {
        return services.AddSingleton<IBlockRunner, BlockRunner>();
    }

    public static IServiceCollection AddDefaultFunctionRunner(this IServiceCollection services)
    {
        return services.AddSingleton<IFunctionRunner, FunctionRunner>();
    }

    public static IServiceCollection AddFunctionFramework<TFunctionFramework>(this IServiceCollection services)
        where TFunctionFramework : class
    {
        return services.AddSingleton<TFunctionFramework>();
    }

    public static IServiceCollection AddBlockFrameworkFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IBlockFrameworkFactory
    {
        return services.AddScoped<IBlockFrameworkFactory, TFactory>();
    }
}
