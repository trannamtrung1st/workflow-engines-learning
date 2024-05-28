using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ILogicRunner<TFramework>
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn>(
        Logic logic, BlockGlobalObject<TFramework> globalObject,
        Guid? optimizationScopeId, CancellationToken cancellationToken);

    Task<IDisposable> Run(
        Logic logic, BlockGlobalObject<TFramework> globalObject,
        Guid? optimizationScopeId, CancellationToken cancellationToken);
}