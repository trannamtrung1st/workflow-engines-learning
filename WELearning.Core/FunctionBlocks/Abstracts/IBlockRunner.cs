using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockRunner<TFramework>
{
    Task<BlockExecutionResult> Run(RunBlockRequest request, BlockExecutionControl control, TFramework blockFramework);
}