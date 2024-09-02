using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IFunctionRunner
{
    Task<(TReturn Result, IOptimizationScope OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, CodeExecutionTracker tracker, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, string optimizationScopeId, RunTokens tokens)
        where TFunctionFramework : IFunctionFramework;

    Task<IOptimizationScope> Run<TFunctionFramework>(
        Function function, CodeExecutionTracker tracker, IBlockFramework blockFramework, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, string optimizationScopeId, RunTokens tokens)
        where TFunctionFramework : IFunctionFramework;

    Task<(TReturn Result, IOptimizationScope OptimizationScope)> Evaluate<TReturn, TArg>(
        Function function, CodeExecutionTracker tracker, TArg arguments, string optimizationScopeId, RunTokens tokens);

    Task<IOptimizationScope> Compile(
        Function function, IEnumerable<string> inputs, IEnumerable<string> outputs,
        IEnumerable<ImportModule> modules, string optimizationScopeId, RunTokens tokens);
}