using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IFunctionRunner
{
    Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, BlockGlobalObject<TFunctionFramework> globalObject,
        IEnumerable<(string Name, object Value)> flattenArguments,
        IEnumerable<string> flattenOutputs, Guid? optimizationScopeId, RunTokens tokens);

    Task<IDisposable> Run<TFunctionFramework>(
        Function function, IBlockFramework blockFramework, BlockGlobalObject<TFunctionFramework> globalObject,
        IEnumerable<(string Name, object Value)> flattenArguments,
        IEnumerable<string> flattenOutputs, Guid? optimizationScopeId, RunTokens tokens);
}