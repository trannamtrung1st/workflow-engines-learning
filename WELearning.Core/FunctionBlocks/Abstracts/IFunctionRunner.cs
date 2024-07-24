using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IFunctionRunner
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, CodeExecutionTracker tracker, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
        where TFunctionFramework : IFunctionFramework;

    Task<IDisposable> Run<TFunctionFramework>(
        Function function, CodeExecutionTracker tracker, IBlockFramework blockFramework, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
        where TFunctionFramework : IFunctionFramework;

    Task<(TReturn Result, IDisposable OptimizationScope)> Evaluate<TReturn, TArg>(
        Function function, CodeExecutionTracker tracker, TArg arguments, Guid? optimizationScopeId, RunTokens tokens);

    Task<IDisposable> Compile(
        Function function, IEnumerable<string> inputs, IEnumerable<string> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens);
}