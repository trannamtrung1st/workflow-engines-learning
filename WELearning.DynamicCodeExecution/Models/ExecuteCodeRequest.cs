using System.Reflection;

namespace WELearning.DynamicCodeExecution.Models;

public class ExecuteCodeRequest<TArg> : CompileCodeRequest
{
    public ExecuteCodeRequest(
        string content, string contentId, TArg arguments, RunTokens tokens,
        IEnumerable<string> imports = null, IEnumerable<Assembly> assemblies = null, IEnumerable<Type> types = null, Type[] extensions = null,
        bool? async = null, IDictionary<string, object> inputs = null, IDictionary<string, object> outputs = null,
        Guid? optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null)
        : base(content, contentId, tokens, imports, assemblies, types, extensions, async, inputs?.Keys, outputs?.Keys, optimizationScopeId, useRawContent, isScriptOnly, modules)
    {
        Arguments = arguments;
        Inputs = inputs; Outputs = outputs;
    }

    public TArg Arguments { get; }
    public new IDictionary<string, object> Inputs { get; }
    public new IDictionary<string, object> Outputs { get; }
}
