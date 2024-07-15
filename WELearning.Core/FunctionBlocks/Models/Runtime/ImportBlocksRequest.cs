using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ImportBlocksRequest
{
    public ImportBlocksRequest(IEnumerable<BasicBlockDef> importBlocks, string moduleName)
    {
        ImportBlocks = importBlocks;
        ModuleName = moduleName ?? FunctionDefaults.ModuleFunctions;
    }

    public IEnumerable<BasicBlockDef> ImportBlocks { get; }
    public string ModuleName { get; }
}
