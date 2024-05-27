using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ILogicRunner<TFramework>
{
    Task<TReturn> Run<TReturn>(Logic logic, BlockGlobalObject<TFramework> globalObject, CancellationToken cancellationToken);
    Task Run(Logic logic, BlockGlobalObject<TFramework> globalObject, CancellationToken cancellationToken);
}