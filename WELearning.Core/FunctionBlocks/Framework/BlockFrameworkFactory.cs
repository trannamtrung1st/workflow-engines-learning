using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkFactory : IBlockFrameworkFactory<IBlockFrameworkInstance>
{
    public IBlockFramework<IBlockFrameworkInstance> Create(BlockExecutionControl control) => new BlockFramework(control);
}
