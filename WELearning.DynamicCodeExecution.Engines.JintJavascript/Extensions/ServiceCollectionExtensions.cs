using Microsoft.Extensions.DependencyInjection;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Engines;
using WELearning.DynamicCodeExecution.Engines.JintJavascript.Models;

namespace WELearning.DynamicCodeExecution.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJintJavascriptEngine(this IServiceCollection services, Action<JintOptions> configure)
        => services.AddSingleton<IRuntimeEngine, JintJavascriptEngine>().Configure(configure);
}
