using Microsoft.Extensions.DependencyInjection;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDefaultProcessRunner(this IServiceCollection services)
    {
        return services.AddTransient<IProcessRunner, ProcessRunner>();
    }

    public static IServiceCollection AddDefaultBlockRunner(this IServiceCollection services)
    {
        return services;
    }
}
