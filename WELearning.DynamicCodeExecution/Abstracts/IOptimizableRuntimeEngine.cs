
using System.Reflection;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IOptimizableRuntimeEngine : IRuntimeEngine
{
    Task CompleteOptimizationScope(Guid id);
    Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid optimizationScopeId, CancellationToken cancellationToken);
    Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid optimizationScopeId, CancellationToken cancellationToken);
}