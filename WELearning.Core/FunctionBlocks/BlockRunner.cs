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

    public async Task<BlockExecutionResult> Run(RunBlockRequest request, IBlockExecutionControl control, TFramework blockFramework)
    {
        var block = request.Block;
        var globalObject = new BlockGlobalObject<TFramework>(blockFramework);
        var blockExecutionResult = await control.Execute(
            triggerEvent: request.TriggerEvent,
            EvaluateCondition: (condition) => _logicRunner.Run<bool>(condition, globalObject: globalObject),
            RunAction: (actionLogic) => _logicRunner.Run(actionLogic, globalObject),
            GetOutputEvents: () => blockFramework.OutputEvents
        );
        return blockExecutionResult;
    }
}