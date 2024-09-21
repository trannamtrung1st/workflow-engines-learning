using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ImportModuleData
{
    public ImportModuleData(string id, IEnumerable<BasicBlockDef> importBlocks, string moduleName)
    {
        Id = id;
        ImportBlocks = importBlocks;
        ModuleName = moduleName ?? FunctionDefaults.ModuleFunctions;
    }

    public string Id { get; }
    public IEnumerable<BasicBlockDef> ImportBlocks { get; }
    public string ModuleName { get; }

    public ImportModule ToImportModule()
    {
        var functions = ImportBlocks
            .Where(b => b.Functions?.Any() == true)
            .SelectMany(b => b.GetModuleFunctions()).ToArray();
        var module = new ImportModule(id: Id, moduleName: ModuleName, functions);
        return module;
    }

    public ImportModuleRef ToImportModuleRef() => new(
        Id: Id,
        ModuleName: ModuleName,
        BlockIds: ImportBlocks?.Select(b => b.Id).ToArray()
    );
}
