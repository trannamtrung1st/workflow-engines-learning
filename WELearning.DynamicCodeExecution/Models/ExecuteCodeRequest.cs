using System.Reflection;

namespace WELearning.DynamicCodeExecution.Models;

public class ExecuteCodeRequest : CompileCodeRequest
{
    public ExecuteCodeRequest(
        string content, string contentId, RunTokens tokens,
        IEnumerable<string> imports = null, IEnumerable<Assembly> assemblies = null, IEnumerable<Type> types = null, Type[] extensions = null,
        bool? async = null, IDictionary<string, object> inputs = null, IDictionary<string, object> outputs = null,
        string optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null, CodeExecutionTracker tracker = null)
        : base(content, contentId, tokens, imports, assemblies, types, extensions, async, inputs?.Keys, outputs?.Keys, optimizationScopeId, useRawContent, isScriptOnly, modules)
    {
        Inputs = inputs;
        Outputs = outputs;
        Tracker = tracker;
    }

    public new IDictionary<string, object> Inputs { get; }
    public new IDictionary<string, object> Outputs { get; }
    public CodeExecutionTracker Tracker { get; }
}

public class ExecuteCodeRequest<TArg> : ExecuteCodeRequest
{
    public ExecuteCodeRequest(
        string content, string contentId, TArg arguments, RunTokens tokens,
        IEnumerable<string> imports = null, IEnumerable<Assembly> assemblies = null, IEnumerable<Type> types = null, Type[] extensions = null,
        bool? async = null, IDictionary<string, object> inputs = null, IDictionary<string, object> outputs = null,
        string optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null, CodeExecutionTracker tracker = null)
        : base(content, contentId, tokens, imports, assemblies, types, extensions, async, inputs, outputs, optimizationScopeId, useRawContent, isScriptOnly, modules, tracker)
    {
        Arguments = arguments;
    }

    public TArg Arguments { get; }
}
