using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Engines;
using WELearning.DynamicCodeExecution.Engines.V8Javascript.Models;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddV8JavascriptEngine(this IServiceCollection services, Action<V8Options> configure)
        => services.AddTransient<IRuntimeEngine, V8JavascriptEngine>().Configure(configure);
}
