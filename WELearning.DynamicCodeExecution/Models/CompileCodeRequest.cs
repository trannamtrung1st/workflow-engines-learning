using System.Reflection;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Models;

public class CompileCodeRequest
{
    public CompileCodeRequest(
        string content, string contentId, RunTokens tokens,
        IEnumerable<string> imports = null, IEnumerable<Assembly> assemblies = null, IEnumerable<Type> types = null, Type[] extensions = null,
        bool? async = null, IEnumerable<string> inputs = null, IEnumerable<string> outputs = null,
        string optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null)
    {
        Content = content;
        ContentId = contentId;
        Inputs = inputs; Outputs = outputs;
        Tokens = tokens;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Extensions = extensions;
        Async = async ?? SyntaxHelper.HasAsyncSyntax(content);
        OptimizationScopeId = optimizationScopeId;
        UseRawContent = useRawContent;
        IsScriptOnly = isScriptOnly;
        Modules = modules;
    }

    public string ContentId { get; } // [NOTE] should be refreshed for new versions
    public string Content { get; }
    public bool Async { get; }
    public IEnumerable<string> Inputs { get; }
    public IEnumerable<string> Outputs { get; }
    public IEnumerable<string> Imports { get; }
    public IEnumerable<Assembly> Assemblies { get; }
    public IEnumerable<Type> Types { get; }
    public Type[] Extensions { get; }
    public string OptimizationScopeId { get; }
    public RunTokens Tokens { get; }
    public bool UseRawContent { get; }
    public bool IsScriptOnly { get; }
    public IEnumerable<ImportModule> Modules { get; }
}
