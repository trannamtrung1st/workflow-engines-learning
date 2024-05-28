
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks;

public class LogicRunner<TFramework> : ILogicRunner<TFramework>
{
    private readonly IRuntimeEngineFactory _engineFactory;
    private static readonly Assembly[] DefaultAssemblies = new[] { typeof(LogicRunner<TFramework>).Assembly };

    public LogicRunner(IRuntimeEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Run<TReturn>(Logic logic, BlockGlobalObject<TFramework> globalObject, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        var optEngine = engine as IOptimizableRuntimeEngine;
        if (optEngine != null)
        {
            var result = await optEngine.Execute<TReturn, BlockGlobalObject<TFramework>>(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies, types: logic.Types,
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
                assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies, types: logic.Types,
                cancellationToken: cancellationToken
            );
            return (result, null);
        }
    }

    public async Task<IDisposable> Run(Logic logic, BlockGlobalObject<TFramework> globalObject, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        var optEngine = engine as IOptimizableRuntimeEngine;
        if (optEngine != null)
        {
            var optimizationScope = await optEngine.Execute(
                content: logic.Content,
                arguments: globalObject,
                imports: logic.Imports,
                assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies, types: logic.Types,
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
                assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies, types: logic.Types,
                cancellationToken: cancellationToken
            );
            return null;
        }
    }
}