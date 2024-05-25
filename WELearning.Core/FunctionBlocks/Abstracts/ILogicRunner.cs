using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ILogicRunner
{
    Task<TReturn> Run<TReturn>(Logic logic, BlockGlobalObject globalObject = null, CancellationToken cancellationToken = default);
    Task Run(Logic logic, BlockGlobalObject globalObject = null, CancellationToken cancellationToken = default);
}