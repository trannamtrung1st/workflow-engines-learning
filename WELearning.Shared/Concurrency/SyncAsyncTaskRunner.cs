using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private readonly ISyncAsyncTaskLimiter _taskLimiter;

    public SyncAsyncTaskRunner(ISyncAsyncTaskLimiter taskLimiter)
    {
        _taskLimiter = taskLimiter;
    }

    public async Task RunSyncAsync(long rateCount, Func<IDisposable, Task> task, bool longRunning = true)
    {
        if (_taskLimiter.TryAcquire(rateCount, out var scope))
            await RunAsyncCore(rateCount, task, longRunning, scope);
        else
            await task(new SimpleScope());
    }

    public async Task RunAsync(long rateCount, Func<IDisposable, Task> task, bool longRunning = true)
    {
        var scope = _taskLimiter.Acquire(rateCount);
        await RunAsyncCore(rateCount, task, longRunning, scope);
    }

    protected virtual async Task RunAsyncCore(long rateCount, Func<IDisposable, Task> task, bool longRunning, IDisposable scope)
    {
        Task asyncTask = null;
        Task MainTask() => task(new SimpleScope(onDispose: () =>
        {
            using var _ = scope;
            asyncTask?.Dispose();
        }));

        if (longRunning)
        {
            asyncTask = Task.Factory.StartNew(
                function: MainTask,
                creationOptions: TaskCreationOptions.LongRunning);
        }
        else
            await Task.Run(MainTask);
    }
}