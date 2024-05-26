using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultProcessRunner<TFrameworkInstance>(this IServiceCollection services)
    {
        return services.AddTransient<IProcessRunner<TFrameworkInstance>, ProcessRunner<TFrameworkInstance>>();
    }

    public static IServiceCollection AddDefaultBlockRunner<TFrameworkInstance>(this IServiceCollection services)
        where TFrameworkInstance : IBlockFrameworkInstance
    {
        return services.AddTransient<IBlockRunner<TFrameworkInstance>, BlockRunner<TFrameworkInstance>>();
    }

    public static IServiceCollection AddDefaultLogicRunner<TFrameworkInstance>(this IServiceCollection services)
    {
        return services.AddTransient<ILogicRunner<TFrameworkInstance>, LogicRunner<TFrameworkInstance>>();
    }

    public static IServiceCollection AddBlockFrameworkFactory<TFrameworkInstance, TBlockFrameworkFactory>(this IServiceCollection services)
        where TBlockFrameworkFactory : class, IBlockFrameworkFactory<TFrameworkInstance>
    {
        return services.AddTransient<IBlockFrameworkFactory<TFrameworkInstance>, TBlockFrameworkFactory>();
    }
}
