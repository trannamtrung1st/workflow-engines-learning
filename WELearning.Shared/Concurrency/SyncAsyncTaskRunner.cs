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
            _ = Task.Factory.StartNew(function: () => task(scope), creationOptions: TaskCreationOptions.LongRunning);
        else
            await task(new SimpleScope());
    }
}