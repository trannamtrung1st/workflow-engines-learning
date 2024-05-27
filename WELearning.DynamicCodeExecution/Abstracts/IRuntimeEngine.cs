
using System.Reflection;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngine
{
    Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken = default);
    Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken = default);
    bool CanRun(ERuntime runtime);
}