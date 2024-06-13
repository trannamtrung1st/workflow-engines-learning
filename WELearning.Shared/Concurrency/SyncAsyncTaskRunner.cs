using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private readonly IDynamicRateLimiter _dynamicRateLimiter;

    public SyncAsyncTaskRunner(IDynamicRateLimiter dynamicRateLimiter)
    {
        _dynamicRateLimiter = dynamicRateLimiter;
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> task, CancellationToken cancellationToken)
    {
        if (_dynamicRateLimiter.TryAcquire(out var scope, cancellationToken))
        {
            _ = Task.Factory.StartNew(
                function: () => task(scope),
                creationOptions: TaskCreationOptions.LongRunning);
        }
        else
            await task(new SimpleScope());
    }
}