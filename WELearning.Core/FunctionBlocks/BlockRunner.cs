using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner : IBlockRunner
{
    public async Task Run(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId)
    {
        bool started = false;
        while (!started)
        {
            control.WaitForIdle(request.Tokens.Combined);
            await control.MutexAccess(async () =>
            {
                if (control.IsIdle)
                {
                    await control.Execute(request, optimizationScopeId);
                    started = true;
                }
            }, request.Tokens.Combined);
        }
    }

    public async Task RunAndWait(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId)
    {
        var tcs = new TaskCompletionSource();
        control.Completed += (o, e) => tcs.SetResult();
        control.Failed += (o, e) => tcs.SetException(e);
        await Run(request, control, optimizationScopeId);
        await tcs.Task;
    }
}