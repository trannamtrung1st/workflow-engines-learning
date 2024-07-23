using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.Extensions;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultBlockRunner(this IServiceCollection services)
    {
        return services.AddSingleton<IBlockRunner, BlockRunner>();
    }

    public static IServiceCollection AddDefaultFunctionRunner(this IServiceCollection services)
    {
        return services.AddSingleton<IFunctionRunner, FunctionRunner>();
    }

    public static IServiceCollection AddBlockFrameworkFactory<TFactory>(this IServiceCollection services)
        where TFactory : class, IBlockFrameworkFactory
    {
        return services.AddScoped<IBlockFrameworkFactory, TFactory>();
    }

    public static IServiceCollection AddFunctionFrameworkFactory<TFunctionFramework, TFactory>(this IServiceCollection services)
        where TFactory : class, IFunctionFrameworkFactory<TFunctionFramework>
        where TFunctionFramework : IFunctionFramework
    {
        return services.AddSingleton<IFunctionFrameworkFactory<TFunctionFramework>, TFactory>();
    }
}
