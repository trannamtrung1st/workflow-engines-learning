
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks;

public class LogicRunner<TFramework> : ILogicRunner<TFramework>
{
    private readonly IRuntimeEngineFactory _engineFactory;
    private readonly ITypeProvider _typeProvider;
    private static readonly Assembly[] DefaultAssemblies = new[] { typeof(LogicRunner<TFramework>).Assembly };

    public LogicRunner(IRuntimeEngineFactory engineFactory, ITypeProvider typeProvider)
    {
        _engineFactory = engineFactory;
        _typeProvider = typeProvider;
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn>(Logic logic, BlockGlobalObject<TFramework> globalObject, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        var optEngine = engine as IOptimizableRuntimeEngine;
        var assemblies = logic.Assemblies != null ? _typeProvider.GetAssemblies(logic.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = logic.Types != null ? _typeProvider.GetTypes(logic.Types) : null;
        if (optEngine != null)
        {
            var result = await optEngine.Execute<TReturn, BlockGlobalObject<TFramework>>(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies, types,
                optimizationScopeId: optimizationScopeId,
                cancellationToken: cancellationToken
            );
            return result;
        }
        else
        {
            var result = await engine.Execute<TReturn, BlockGlobalObject<TFramework>>(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies, types,
                cancellationToken: cancellationToken
            );
            return (result, null);
        }
    }

    public async Task<IDisposable> Run(Logic logic, BlockGlobalObject<TFramework> globalObject, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        var optEngine = engine as IOptimizableRuntimeEngine;
        var assemblies = logic.Assemblies != null ? _typeProvider.GetAssemblies(logic.Assemblies) : null;
        assemblies = assemblies != null ? assemblies.Concat(DefaultAssemblies) : DefaultAssemblies;
        var types = logic.Types != null ? _typeProvider.GetTypes(logic.Types) : null;
        if (optEngine != null)
        {
            var optimizationScope = await optEngine.Execute(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies, types,
                optimizationScopeId: optimizationScopeId,
                cancellationToken: cancellationToken
            );
            return optimizationScope;
        }
        else
        {
            await engine.Execute(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies, types,
                cancellationToken: cancellationToken
            );
            return null;
        }
    }
}