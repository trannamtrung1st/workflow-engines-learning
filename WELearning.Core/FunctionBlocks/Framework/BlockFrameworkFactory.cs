using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkFactory : IBlockFrameworkFactory<IBlockFramework>
{
    public IBlockFramework Create(IBlockExecutionControl control) => new BlockFramework(control);
}
