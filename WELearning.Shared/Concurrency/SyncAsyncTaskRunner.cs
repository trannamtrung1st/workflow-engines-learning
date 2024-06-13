using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private static readonly int DefaultMaxAsync = Environment.ProcessorCount * 2;
    private readonly object _syncLock;
    private long _asyncCount;

    public SyncAsyncTaskRunner()
    {
        _syncLock = new();
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> task)
    {
        bool canRunAsync = false;
        lock (_syncLock)
        {
            if (_asyncCount < DefaultMaxAsync)
            {
                canRunAsync = true;
                _asyncCount++;
            }
        }

        if (canRunAsync)
        {
            _ = Task.Factory.StartNew(
                function: () => task(new SimpleScope(onDispose: () => Interlocked.Decrement(ref _asyncCount))),
                creationOptions: TaskCreationOptions.LongRunning);
        }
        else
            await task(new SimpleScope());
    }
}