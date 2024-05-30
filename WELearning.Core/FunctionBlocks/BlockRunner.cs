using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner<TFramework> : IBlockRunner<TFramework> where TFramework : IBlockFramework
{
    public async Task<BlockExecutionResult> Run(
        RunBlockRequest request, IBlockExecutionControl control,
        Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        BlockExecutionResult result = null;
        while (result == null)
        {
            control.WaitForIdle(cancellationToken);
            await control.MutexAccess(async () =>
            {
                if (control.IsIdle)
                    result = await control.Execute(triggerEvent: request.TriggerEvent,
                        bindings: request.Bindings, optimizationScopeId, cancellationToken: cancellationToken);
            }, cancellationToken);
        }
        return result;
    }
}