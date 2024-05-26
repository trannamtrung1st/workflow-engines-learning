using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ILogicRunner<TFrameworkInstance>
{
    Task<TReturn> Run<TReturn>(Logic logic, BlockGlobalObject<TFrameworkInstance> globalObject = null, CancellationToken cancellationToken = default);
    Task Run(Logic logic, BlockGlobalObject<TFrameworkInstance> globalObject = null, CancellationToken cancellationToken = default);
}