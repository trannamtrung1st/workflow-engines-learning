using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Function
{
    public Function(string id, string name, string content,
        ERuntime runtime, IEnumerable<string> imports,
        IEnumerable<string> assemblies, IEnumerable<string> types,
        bool? async = null, bool useRawContent = false, string signature = null, bool exported = false)
    {
        Id = id;
        Name = name;
        Content = content;
        Runtime = runtime;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
        Async = async ?? SyntaxHelper.HasAsyncSyntax(content);
        UseRawContent = useRawContent;
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
    public bool UseRawContent { get; set; }
    public bool Exported { get; set; }
}