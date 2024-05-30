using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessRunner<TFramework> : IProcessRunner
{
    public virtual async Task Run(RunProcessRequest request, IProcessExecutionControl processControl, CancellationToken cancellationToken)
    {
        bool started = false;
        while (!started)
        {
            processControl.WaitForIdle(cancellationToken);
            await processControl.MutexAccess(async () =>
            {
                if (processControl.IsIdle)
                {
                    await processControl.Execute(triggers: request.Triggers, bindings: request.Bindings, cancellationToken);
                    started = true;
                }
            }, cancellationToken);
        }
    }
}