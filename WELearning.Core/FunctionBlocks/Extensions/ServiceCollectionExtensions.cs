using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultBlockRunner(this IServiceCollection services)
    {
        return services.AddTransient<IBlockRunner, BlockRunner>();
    }

    public static IServiceCollection AddDefaultFunctionRunner<TFramework>(this IServiceCollection services)
    {
        return services.AddTransient<IFunctionRunner<TFramework>, FunctionRunner<TFramework>>();
    }

    public static IServiceCollection AddBlockFrameworkFactory<TFramework, TBlockFrameworkFactory>(this IServiceCollection services)
        where TBlockFrameworkFactory : class, IBlockFrameworkFactory<TFramework>
    {
        return services.AddSingleton<IBlockFrameworkFactory<TFramework>, TBlockFrameworkFactory>();
    }
}
