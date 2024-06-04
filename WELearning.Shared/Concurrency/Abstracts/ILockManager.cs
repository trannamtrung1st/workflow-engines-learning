namespace WELearning.Shared.Concurrency.Abstracts;

public interface ILockManager
{
    ILock CreateLock(string key, TimeSpan? expiry = null, TimeSpan? timeout = null, int retries = 3);
    void MutexAccess(string key, Action action);
    Task MutexAccess(string key, Func<Task> action);
}
