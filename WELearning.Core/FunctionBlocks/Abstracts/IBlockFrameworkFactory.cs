using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFrameworkFactory<TFrameworkInstance>
{
    IBlockFramework<TFrameworkInstance> Create(BlockExecutionControl control);
}