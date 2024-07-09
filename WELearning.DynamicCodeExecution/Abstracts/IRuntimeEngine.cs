using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IRuntimeEngine
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request);
    Task<IDisposable> Execute<TArg>(ExecuteCodeRequest<TArg> request);
    Task<IDisposable> Compile(CompileCodeRequest request);
    bool CanRun(ERuntime runtime);
}
