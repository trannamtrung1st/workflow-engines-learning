using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultProcessRunner<TFramework>(this IServiceCollection services)
    {
        return services.AddTransient<IProcessRunner, ProcessRunner<TFramework>>();
    }

    public static IServiceCollection AddDefaultBlockRunner<TFramework>(this IServiceCollection services)
        where TFramework : IBlockFramework
    {
        return services.AddTransient<IBlockRunner<TFramework>, BlockRunner<TFramework>>();
    }

    public static IServiceCollection AddDefaultLogicRunner<TFramework>(this IServiceCollection services)
    {
        return services.AddTransient<ILogicRunner<TFramework>, LogicRunner<TFramework>>();
    }

    public static IServiceCollection AddBlockFrameworkFactory<TFramework, TBlockFrameworkFactory>(this IServiceCollection services)
        where TBlockFrameworkFactory : class, IBlockFrameworkFactory<TFramework>
    {
        return services.AddTransient<IBlockFrameworkFactory<TFramework>, TBlockFrameworkFactory>();
    }

    public static IServiceCollection AddDefaultTypeProvider(this IServiceCollection services)
    {
        return services.AddTransient<ITypeProvider, TypeProvider>();
    }
}
