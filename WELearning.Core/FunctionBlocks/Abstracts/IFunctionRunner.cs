using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IFunctionRunner<TFramework>
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn>(
        Function function, BlockGlobalObject<TFramework> globalObject,
        Guid? optimizationScopeId, CancellationToken cancellationToken);

    Task<IDisposable> Run(
        Function function, BlockGlobalObject<TFramework> globalObject,
        Guid? optimizationScopeId, CancellationToken cancellationToken);
}