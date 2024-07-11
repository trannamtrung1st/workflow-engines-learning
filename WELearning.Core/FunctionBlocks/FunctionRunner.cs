
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

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn, TFunctionFramework>(
        Function function, BlockGlobalObject<TFunctionFramework> globalObject,
        IDictionary<string, object> inputs, IDictionary<string, object> outputs,
        IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
    {
        var (engine, assemblies, types) = PrepareInputs(function);
        var result = await engine.Execute<TReturn, BlockGlobalObject<TFunctionFramework>>(
            request: new(
                content: function.Content,
                contentId: function.Id,
                arguments: globalObject, tokens,
                imports: function.Imports, assemblies, types, extensions: function.GetExtensions(),
                inputs: inputs, outputs: outputs,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                isScriptOnly: function.IsScriptOnly,
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
        var (engine, assemblies, types) = PrepareInputs(function);
        var (result, scope) = await engine.Execute<dynamic, BlockGlobalObject<TFunctionFramework>>(
            request: new(
                content: function.Content,
                contentId: function.Id,
                arguments: globalObject, tokens,
                imports: function.Imports, assemblies, types, extensions: function.GetExtensions(),
                inputs: inputs, outputs: outputs,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                isScriptOnly: function.IsScriptOnly,
                modules: modules
            )
        );
        blockFramework.HandleDynamicResult(result);
        return scope;
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Evaluate<TReturn, TArg>(Function function, TArg arguments, Guid? optimizationScopeId, RunTokens tokens)
    {
        var (engine, assemblies, types) = PrepareInputs(function);
        var result = await engine.Execute<TReturn, TArg>(
            request: new(
                content: function.Content,
                contentId: function.Id,
                arguments: arguments, tokens,
                imports: function.Imports, assemblies, types, extensions: function.GetExtensions(),
                inputs: null, outputs: null,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                isScriptOnly: function.IsScriptOnly,
                modules: null
            )
        );
        return result;
    }

    private (IRuntimeEngine Engine, IEnumerable<Assembly> Assemblies, IEnumerable<Type> Types) PrepareInputs(Function function)
    {
        var engine = _engineFactory.CreateEngine(runtime: function.Runtime);
        var assemblies = function.Assemblies != null ? _typeProvider.GetAssemblies(function.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = function.Types != null ? _typeProvider.GetTypes(function.Types) : null;
        return (engine, assemblies, types);
    }

    public async Task<IDisposable> Compile(Function function, IEnumerable<string> inputs, IEnumerable<string> outputs, IEnumerable<ImportModule> modules, Guid? optimizationScopeId, RunTokens tokens)
    {
        var (engine, assemblies, types) = PrepareInputs(function);
        var scope = await engine.Compile(
            request: new(
                content: function.Content,
                contentId: function.Id, tokens,
                imports: function.Imports, assemblies, types, extensions: function.GetExtensions(),
                inputs: inputs, outputs: outputs,
                async: function.Async,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent,
                isScriptOnly: function.IsScriptOnly,
                modules: modules
            )
        );
        return scope;
    }
}