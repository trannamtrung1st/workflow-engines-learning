using System.Reflection;

namespace WELearning.DynamicCodeExecution.Models;

public class ExecuteCodeRequest<TArg>
{
    public ExecuteCodeRequest(
        string content, TArg arguments, IEnumerable<(string Name, object Value)> flattenArguments,
        IEnumerable<string> flattenOutputs, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types,
        RunTokens tokens, Guid? optimizationScopeId = default, bool useRawContent = false)
    {
        Content = content;
        Arguments = arguments;
        FlattenArguments = flattenArguments;
        FlattenOutputs = flattenOutputs;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Tokens = tokens;
        OptimizationScopeId = optimizationScopeId;
        UseRawContent = useRawContent;
    }

    public string Content { get; }
    public TArg Arguments { get; }
    public IEnumerable<(string Name, object Value)> FlattenArguments { get; }
    public IEnumerable<string> FlattenOutputs { get; }
    public IEnumerable<string> Imports { get; }
    public IEnumerable<Assembly> Assemblies { get; }
    public IEnumerable<Type> Types { get; }
    public Guid? OptimizationScopeId { get; }
    public RunTokens Tokens { get; }
    public bool UseRawContent { get; }
}