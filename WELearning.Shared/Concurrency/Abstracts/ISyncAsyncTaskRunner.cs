namespace WELearning.Shared.Concurrency.Abstracts;

public interface ISyncAsyncTaskRunner
{
    Task RunSyncAsync(long rateCount, Func<IDisposable, Task> task, bool longRunning = true);
    Task RunAsync(long rateCount, Func<IDisposable, Task> task, bool longRunning = true);
}