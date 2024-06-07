namespace WELearning.DynamicCodeExecution.Models;

public class ImportModule
{
    public ImportModule(string moduleName, IEnumerable<ModuleFunction> functions)
    {
        ModuleName = moduleName;
        Functions = functions;
    }

    public string ModuleName { get; }
    public IEnumerable<ModuleFunction> Functions { get; }
}
