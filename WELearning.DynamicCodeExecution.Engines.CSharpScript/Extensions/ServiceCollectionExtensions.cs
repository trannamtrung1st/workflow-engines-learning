using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Engines;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCSharpScriptEngine(this IServiceCollection services)
        => services.AddSingleton<IRuntimeEngine, CSharpScriptEngine>();
}
