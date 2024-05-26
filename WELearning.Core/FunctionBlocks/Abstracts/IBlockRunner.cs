using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockRunner<TFrameworkInstance>
{
    Task<BlockExecutionResult> Run(RunBlockRequest request, BlockExecutionControl control, IBlockFramework<TFrameworkInstance> blockFramework);
}