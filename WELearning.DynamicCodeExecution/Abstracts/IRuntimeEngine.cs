using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngine
{
    Task<(TReturn Result, IOptimizationScope OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request);
    Task<IOptimizationScope> Execute<TArg>(ExecuteCodeRequest<TArg> request);
    Task<IOptimizationScope> Compile(CompileCodeRequest request);
    bool CanRun(ERuntime runtime);
}
