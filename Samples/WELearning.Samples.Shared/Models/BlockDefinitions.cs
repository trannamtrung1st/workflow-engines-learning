using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Samples.Shared.Models;

public class BlockDefinitions
{
    public CompositeBlockDef Cfb { get; set; }
    public IEnumerable<BasicBlockDef> Bfbs { get; set; }
}