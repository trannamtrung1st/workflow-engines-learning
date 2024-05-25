using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFrameworkFactory
{
    IBlockFramework Create(BlockExecutionControl control);
}