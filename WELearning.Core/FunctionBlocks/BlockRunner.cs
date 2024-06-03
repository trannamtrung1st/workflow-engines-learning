using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner : IBlockRunner
{
    public async Task Run(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        bool started = false;
        while (!started)
        {
            control.WaitForIdle(cancellationToken);
            await control.MutexAccess(async () =>
            {
                if (control.IsIdle)
                {
                    await control.Execute(request, optimizationScopeId, cancellationToken);
                    started = true;
                }
            }, cancellationToken);
        }
    }

    public async Task RunAndWait(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        control.Completed += (o, e) => tcs.SetResult();
        control.Failed += (o, e) => tcs.SetException(e);
        await Run(request, control, optimizationScopeId, cancellationToken);
        await tcs.Task;
    }
}