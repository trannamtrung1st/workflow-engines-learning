using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject
{
    public BlockGlobalObject(IBlockFrameworkInstance frameworkInstance)
    {
        FB = frameworkInstance;
    }

    public IBlockFrameworkInstance FB { get; }
}