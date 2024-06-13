using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IFunctionRunner
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens);

    Task<IDisposable> Run<TFunctionFramework>(
        Function function, IBlockFramework blockFramework, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens);

    Task<(TReturn Result, IDisposable OptimizationScope)> Evaluate<TReturn, TArg>(
        Function function, TArg arguments, Guid? optimizationScopeId, RunTokens tokens);
}