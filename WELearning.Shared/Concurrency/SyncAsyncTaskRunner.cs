using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private static readonly int DefaultMaxAsync = Environment.ProcessorCount * 2;
    private object _syncLock;
    private long _asyncCount;

    public SyncAsyncTaskRunner()
    {
        _syncLock = new();
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> func, TaskCreationOptions creationOptions = default)
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
                async () => await func(new AsyncScope(onDispose: () => Interlocked.Decrement(ref _asyncCount))),
                creationOptions);
        }
        else
            await func(new SyncScope());
    }

    class AsyncScope : IDisposable
    {
        private readonly Action _onDispose;
        public AsyncScope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose() => _onDispose();
    }

    class SyncScope : IDisposable
    {
        public void Dispose() { }
    }
}