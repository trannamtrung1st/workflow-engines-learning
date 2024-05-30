using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner<TFramework> : IBlockRunner<TFramework> where TFramework : IBlockFramework
{
    private readonly ILogicRunner<TFramework> _logicRunner;

    public BlockRunner(ILogicRunner<TFramework> logicRunner)
    {
        _logicRunner = logicRunner;
    }

    // [TODO] refactor runner and control
    public async Task<BlockExecutionResult> Run(
        RunBlockRequest request, IBlockExecutionControl control,
        TFramework blockFramework, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var optimizationScopes = new HashSet<IDisposable>();
        optimizationScopeId ??= Guid.NewGuid();
        var globalObject = new BlockGlobalObject<TFramework>(blockFramework);
        try
        {
            var blockExecutionResult = await control.Execute(
                triggerEvent: request.TriggerEvent,
                EvaluateCondition: async (condition, cancellationToken) =>
                {
                    var (Result, OptimizationScope) = await _logicRunner.Run<bool>(condition, globalObject: globalObject, optimizationScopeId.Value, cancellationToken);
                    if (OptimizationScope != null) optimizationScopes.Add(OptimizationScope);
                    return Result;
                },
                RunAction: async (actionLogic, cancellationToken) =>
                {
                    var optimizationScope = await _logicRunner.Run(actionLogic, globalObject, optimizationScopeId.Value, cancellationToken);
                    if (optimizationScope != null) optimizationScopes.Add(optimizationScope);
                },
                GetOutputEvents: () => blockFramework.OutputEvents,
                cancellationToken: cancellationToken
            );
            return blockExecutionResult;
        }
        finally
        {
            foreach (var optimizationScope in optimizationScopes)
                optimizationScope.Dispose();
        }
    }
}