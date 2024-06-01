
using System.Reflection;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IOptimizableRuntimeEngine : IRuntimeEngine
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(
        string content, TArg arguments, IEnumerable<(string Name, object Value)> flattenArguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies,
        IEnumerable<Type> types, Guid? optimizationScopeId, CancellationToken cancellationToken);

    Task<IDisposable> Execute<TArg>(
        string content, TArg arguments, IEnumerable<(string Name, object Value)> flattenArguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies,
        IEnumerable<Type> types, Guid? optimizationScopeId, CancellationToken cancellationToken);
}