using System.Reflection;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Models;

public class ExecuteCodeRequest<TArg>
{
    public ExecuteCodeRequest(
        string content, TArg arguments, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies, IEnumerable<Type> types, RunTokens tokens, bool? async = null,
        IDictionary<string, object> inputs = null, IDictionary<string, object> outputs = null,
        Guid? optimizationScopeId = default, bool useRawContent = false,
        IEnumerable<ImportModule> modules = null)
    {
        Content = content;
        Arguments = arguments;
        Inputs = inputs; Outputs = outputs;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Tokens = tokens;
        Async = async ?? SyntaxHelper.HasAsyncSyntax(content);
        OptimizationScopeId = optimizationScopeId;
        UseRawContent = useRawContent;
        Modules = modules;
    }

    public string Content { get; }
    public bool Async { get; }
    public TArg Arguments { get; }
    public IDictionary<string, object> Inputs { get; }
    public IDictionary<string, object> Outputs { get; }
    public IEnumerable<string> Imports { get; }
    public IEnumerable<Assembly> Assemblies { get; }
    public IEnumerable<Type> Types { get; }
    public Guid? OptimizationScopeId { get; }
    public RunTokens Tokens { get; }
    public bool UseRawContent { get; }
    public IEnumerable<ImportModule> Modules { get; }
}
