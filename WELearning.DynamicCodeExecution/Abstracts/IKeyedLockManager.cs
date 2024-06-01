namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IKeyedLockManager
{
    void Acquire(string key);
    void Release(string key);
    void MutexAccess(string key, Action action);
    Task MutexAccess(string key, Func<Task> action);
}