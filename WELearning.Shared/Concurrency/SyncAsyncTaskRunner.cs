using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private readonly ISyncAsyncTaskLimiter _taskLimiter;

    public SyncAsyncTaskRunner(ISyncAsyncTaskLimiter taskLimiter)
    {
        _taskLimiter = taskLimiter;
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> task, CancellationToken cancellationToken)
    {
        if (_taskLimiter.TryAcquire(out var scope, cancellationToken))
        {
            Task asyncTask = null;
            asyncTask = Task.Factory.StartNew(
                function: () => task(new SimpleScope(onDispose: () =>
                {
                    scope.Dispose();
                    asyncTask.Dispose();
                })),
                creationOptions: TaskCreationOptions.LongRunning);
        }
        else
            await task(new SimpleScope());
    }
}