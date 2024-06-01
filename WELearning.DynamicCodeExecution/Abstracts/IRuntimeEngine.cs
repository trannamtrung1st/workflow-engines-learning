
using System.Reflection;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngine
{
    Task<TReturn> Execute<TReturn, TArg>(
        string content, TArg arguments, IEnumerable<(string Name, object Value)> flattenArguments, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken);
    Task Execute<TArg>(string content, TArg arguments, IEnumerable<(string Name, object Value)> flattenArguments, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken);
    bool CanRun(ERuntime runtime);
}
