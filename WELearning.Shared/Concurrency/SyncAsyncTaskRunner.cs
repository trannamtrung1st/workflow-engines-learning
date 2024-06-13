using Microsoft.Extensions.Configuration;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskRunner : ISyncAsyncTaskRunner
{
    private readonly object _syncLock;
    private readonly int _maxAsync;
    private long _asyncCount;

    public SyncAsyncTaskRunner(IConfiguration configuration)
    {
        _maxAsync = configuration.GetValue<int>("AppSettings:TaskRunnerMaxAsync");
        _syncLock = new();
    }

    public async Task TryRunTaskAsync(Func<IDisposable, Task> task)
    {
        bool canRunAsync = false;
        lock (_syncLock)
        {
            if (_asyncCount < _maxAsync)
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