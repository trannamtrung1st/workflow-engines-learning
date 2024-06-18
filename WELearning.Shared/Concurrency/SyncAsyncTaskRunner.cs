using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private readonly ISyncAsyncTaskLimiter _taskLimiter;

    public SyncAsyncTaskRunner(ISyncAsyncTaskLimiter taskLimiter)
    {
        _taskLimiter = taskLimiter;
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> task)
    {
        if (_taskLimiter.TryAcquire(out var scope))
        {
            Task asyncTask = null;
            asyncTask = Task.Factory.StartNew(
                function: () => task(new SimpleScope(onDispose: () =>
                {
                    using var _ = scope;
                    using var _1 = asyncTask;
                })),
                creationOptions: TaskCreationOptions.LongRunning);
        }
        else
            await task(new SimpleScope());
    }
}