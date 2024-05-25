using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockRunner
{
    Task<BlockExecutionResult> Run(RunBlockRequest request, BlockExecutionControl control, IBlockFramework blockFramework);
}