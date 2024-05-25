using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultProcessRunner(this IServiceCollection services)
    {
        return services.AddTransient<IProcessRunner, ProcessRunner>();
    }

    public static IServiceCollection AddDefaultBlockRunner(this IServiceCollection services)
    {
        return services.AddTransient<IBlockRunner, BlockRunner>();
    }

    public static IServiceCollection AddDefaultLogicRunner(this IServiceCollection services)
    {
        return services.AddTransient<ILogicRunner, LogicRunner>();
    }

    public static IServiceCollection AddDefaultBlockFrameworkFactory(this IServiceCollection services)
    {
        return services.AddTransient<IBlockFrameworkFactory, BlockFrameworkFactory>();
    }
}
