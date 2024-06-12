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
}