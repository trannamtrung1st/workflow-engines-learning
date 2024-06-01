using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Function
{
    public Function(string id, string name, string content,
        ERuntime runtime, IEnumerable<string> imports,
        IEnumerable<string> assemblies, IEnumerable<string> types)
    {
        Id = id;
        Name = name;
        Content = content;
        Runtime = runtime;
        Imports = imports;
        Assemblies = assemblies;
        Types = types;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public ERuntime Runtime { get; set; }
    public IEnumerable<string> Imports { get; set; }
    public IEnumerable<string> Assemblies { get; set; }
    public IEnumerable<string> Types { get; set; }
}