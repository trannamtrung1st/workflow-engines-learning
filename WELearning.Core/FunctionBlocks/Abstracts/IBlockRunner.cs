using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockRunner<TFramework>
{
    Task<BlockExecutionResult> Run(RunBlockRequest request, IBlockExecutionControl control, TFramework blockFramework, CancellationToken cancellationToken);
}