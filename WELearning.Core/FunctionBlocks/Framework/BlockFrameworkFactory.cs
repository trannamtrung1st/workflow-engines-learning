using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkFactory : IBlockFrameworkFactory<IBlockFramework>
{
    public IBlockFramework Create(IBasicEC control) => new BlockFramework(control);
}
