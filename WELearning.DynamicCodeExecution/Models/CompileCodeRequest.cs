using System.Reflection;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Models;

public class CompileCodeRequest
{
    public CompileCodeRequest(
        string content, string contentId, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies, IEnumerable<Type> types, RunTokens tokens, bool? async = null,
        IEnumerable<string> inputs = null, IEnumerable<string> outputs = null,
        Guid? optimizationScopeId = default, bool useRawContent = false, bool isScriptOnly = false,
        IEnumerable<ImportModule> modules = null)
    {
        Content = content;
        ContentId = contentId;
        Inputs = inputs; Outputs = outputs;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Tokens = tokens;
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
    public Guid? OptimizationScopeId { get; }
    public RunTokens Tokens { get; }
    public bool UseRawContent { get; }
    public bool IsScriptOnly { get; }
    public IEnumerable<ImportModule> Modules { get; }
}
