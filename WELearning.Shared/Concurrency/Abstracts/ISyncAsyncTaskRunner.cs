namespace WELearning.Shared.Concurrency.Abstracts;

public interface ISyncAsyncTaskRunner
{
    Task TryRunTaskAsync(Func<IDisposable, Task> task, CancellationToken cancellationToken);
}