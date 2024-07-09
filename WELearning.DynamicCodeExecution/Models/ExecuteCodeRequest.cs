using System.Reflection;

namespace WELearning.DynamicCodeExecution.Models;

public class ExecuteCodeRequest<TArg> : CompileCodeRequest
{
    public ExecuteCodeRequest(
        string content, string contentId, TArg arguments, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies, IEnumerable<Type> types, RunTokens tokens, bool? async = null,
        IDictionary<string, object> inputs = null, IDictionary<string, object> outputs = null,
        Guid? optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null)
        : base(content, contentId, imports, assemblies, types, tokens, async, inputs?.Keys, outputs?.Keys, optimizationScopeId, useRawContent, isScriptOnly, modules)
    {
        Arguments = arguments;
        Inputs = inputs; Outputs = outputs;
    }

    public TArg Arguments { get; }
    public new IDictionary<string, object> Inputs { get; }
    public new IDictionary<string, object> Outputs { get; }
}
