
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.Core.Reflection.Abstracts;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks;

public class FunctionRunner<TFramework> : IFunctionRunner<TFramework>
{
    private readonly IRuntimeEngineFactory _engineFactory;
    private readonly ITypeProvider _typeProvider;
    private static readonly Assembly[] DefaultAssemblies = new[] { typeof(FunctionRunner<TFramework>).Assembly };

    public FunctionRunner(IRuntimeEngineFactory engineFactory, ITypeProvider typeProvider)
    {
        _engineFactory = engineFactory;
        _typeProvider = typeProvider;
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn>(
        Function function, BlockGlobalObject<TFramework> globalObject, IEnumerable<(string Name, object Value)> flattenArguments,
        Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: function.Runtime);
        var assemblies = function.Assemblies != null ? _typeProvider.GetAssemblies(function.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = function.Types != null ? _typeProvider.GetTypes(function.Types) : null;
        var result = await engine.Execute<TReturn, BlockGlobalObject<TFramework>>(
            request: new(
                content: function.Content,
                arguments: globalObject,
                flattenArguments: flattenArguments,
                imports: function.Imports,
                assemblies, types,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent
            ),
            cancellationToken: cancellationToken
        );
        return result;
    }

    public async Task<IDisposable> Run(
        Function function, BlockGlobalObject<TFramework> globalObject, IEnumerable<(string Name, object Value)> flattenArguments,
        Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: function.Runtime);
        var assemblies = function.Assemblies != null ? _typeProvider.GetAssemblies(function.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = function.Types != null ? _typeProvider.GetTypes(function.Types) : null;
        var scope = await engine.Execute<BlockGlobalObject<TFramework>>(
            request: new(
                content: function.Content,
                arguments: globalObject,
                flattenArguments: flattenArguments,
                imports: function.Imports,
                assemblies, types,
                optimizationScopeId: optimizationScopeId,
                useRawContent: function.UseRawContent
            ),
            cancellationToken: cancellationToken
        );
        return scope;
    }
}