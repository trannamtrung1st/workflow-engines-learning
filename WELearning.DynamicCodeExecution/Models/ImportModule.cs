namespace WELearning.DynamicCodeExecution.Models;

public class ImportModule
{
    public ImportModule(string id, string moduleName, IEnumerable<ModuleFunction> functions)
    {
        Id = id;
        ModuleName = moduleName;
        Functions = functions;
    }

    public string Id { get; set; }
    public string ModuleName { get; }
    public IEnumerable<ModuleFunction> Functions { get; }
}
