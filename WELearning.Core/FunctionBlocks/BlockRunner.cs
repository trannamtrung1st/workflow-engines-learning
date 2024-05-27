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

    public async Task<BlockExecutionResult> Run(RunBlockRequest request, IBlockExecutionControl control, TFramework blockFramework, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var block = request.Block;
        optimizationScopeId ??= Guid.NewGuid();
        var globalObject = new BlockGlobalObject<TFramework>(blockFramework);
        try
        {
            var blockExecutionResult = await control.Execute(
                triggerEvent: request.TriggerEvent,
                EvaluateCondition: (condition, cancellationToken) => _logicRunner.Run<bool>(condition, globalObject: globalObject, optimizationScopeId.Value, cancellationToken),
                RunAction: (actionLogic, cancellationToken) => _logicRunner.Run(actionLogic, globalObject, optimizationScopeId.Value, cancellationToken),
                GetOutputEvents: () => blockFramework.OutputEvents,
                cancellationToken: cancellationToken
            );
            return blockExecutionResult;
        }
        finally { await _logicRunner.CompleteOptimizationScope(optimizationScopeId.Value); }
    }
}