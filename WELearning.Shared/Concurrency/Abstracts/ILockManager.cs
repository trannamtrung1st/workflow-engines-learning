namespace WELearning.Shared.Concurrency.Abstracts;

public interface ILockManager
{
    ILock Acquire(string key, TimeSpan? expiry = null, TimeSpan? timeout = null, int retries = 3);
    void MutexAccess(string key, Action action);
    Task MutexAccess(string key, Func<Task> action);
}