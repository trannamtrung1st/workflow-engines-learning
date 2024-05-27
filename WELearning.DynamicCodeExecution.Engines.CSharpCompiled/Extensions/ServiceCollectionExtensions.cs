using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Engines;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCSharpCompiledEngine(this IServiceCollection services)
        => services.AddTransient<IRuntimeEngine, CSharpCompiledEngine>();
}
