using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Function
{
    public Function()
    {
    }

    public Function(string id, string name, string content, ERuntime runtime,
        IEnumerable<string> imports = null, IEnumerable<string> assemblies = null,
        IEnumerable<string> types = null, string[] extensions = null,
        bool? async = null, bool useRawContent = false, bool isScriptOnly = false,
        string signature = null, bool exported = false)
    {
        Id = id;
        Name = name;
        Content = content;
        Runtime = runtime;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Extensions = extensions;
        Async = async ?? SyntaxHelper.HasAsyncSyntax(content);
        UseRawContent = useRawContent;
        IsScriptOnly = isScriptOnly;
        Signature = signature;
        Exported = exported;
    }

    public string Id { get; set; }
    public string Signature { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public bool Async { get; set; }
    public ERuntime Runtime { get; set; }
    public IEnumerable<string> Imports { get; set; }
    public IEnumerable<string> Assemblies { get; set; }
    public IEnumerable<string> Types { get; set; }
    private Type[] _extensions;
    public string[] Extensions { get; set; }
    public bool UseRawContent { get; set; }
    public bool IsScriptOnly { get; set; }
    public bool Exported { get; set; }

    public Type[] GetExtensions()
    {
        if (_extensions == null)
            _extensions = Extensions.Select(Type.GetType).ToArray();
        return _extensions;
    }

    public static Function CreateRawExpression(string content, ERuntime runtime)
    {
        var randomId = Guid.NewGuid().ToString();
        return new Function(
            id: randomId, name: randomId, content, runtime,
            useRawContent: true, isScriptOnly: true, exported: false
        );
    }
}