
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.Core.Reflection.Abstracts;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks;

public class FunctionRunner : IFunctionRunner
{
    private readonly IRuntimeEngineFactory _engineFactory;
    private readonly ITypeProvider _typeProvider;
    private static readonly Assembly[] DefaultAssemblies = new[] { typeof(FunctionRunner).Assembly };

    public FunctionRunner(IRuntimeEngineFactory engineFactory, ITypeProvider typeProvider)
    {
        _engineFactory = engineFactory;
        _typeProvider = typeProvider;
    }

    // [TODO] optimize models
    public async Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
    {
        var engine = _engineFactory.CreateEngine(runtime: function.Runtime);
        var assemblies = function.Assemblies != null ? _typeProvider.GetAssemblies(function.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = function.Types != null ? _typeProvider.GetTypes(function.Types) : null;
        var result = await engine.Execute<TReturn, BlockGlobalObject<TFunctionFramework>>(
            request: new(
                content: function.Content,
                contentId: function.Id,
                arguments: globalObject,
                imports: function.Imports,
                assemblies, types, tokens,
                inputs: inputs, outputs: outputs,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                modules: modules
            )
        );
        return result;
    }

    public async Task<IDisposable> Run<TFunctionFramework>(
        Function function, IBlockFramework blockFramework, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
    {
        var engine = _engineFactory.CreateEngine(runtime: function.Runtime);
        var assemblies = function.Assemblies != null ? _typeProvider.GetAssemblies(function.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = function.Types != null ? _typeProvider.GetTypes(function.Types) : null;
        var (result, scope) = await engine.Execute<dynamic, BlockGlobalObject<TFunctionFramework>>(
            request: new(
                content: function.Content,
                contentId: function.Id,
                arguments: globalObject,
                imports: function.Imports,
                assemblies, types, tokens,
                inputs: inputs, outputs: outputs,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                modules: modules
            )
        );
        blockFramework.HandleDynamicResult(result);
        return scope;
    }
}