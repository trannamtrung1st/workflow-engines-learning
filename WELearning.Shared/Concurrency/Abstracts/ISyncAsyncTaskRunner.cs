namespace WELearning.Shared.Concurrency.Abstracts;

public interface ISyncAsyncTaskRunner
{
    Task TryRunTaskAsync(long rateCount, Func<IDisposable, Task> task);
}