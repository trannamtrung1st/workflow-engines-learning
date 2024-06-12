namespace WELearning.Shared.Concurrency.Abstracts;

public interface ISyncAsyncTaskRunner
{
    Task TryRunTaskAsync(Func<IDisposable, Task> func, TaskCreationOptions creationOptions = default);
}