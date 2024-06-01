using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockRunner
{
    Task Run(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId, CancellationToken cancellationToken);
}