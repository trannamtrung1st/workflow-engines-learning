using System.Collections.Concurrent;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution;

public class KeyedLockManager : IKeyedLockManager
{
    private readonly ConcurrentDictionary<string, LockObject> _lockMap;

    public KeyedLockManager()
    {
        _lockMap = new();
    }

    public void Acquire(string key)
    {
        LockObject lockObj;
        lock (_lockMap)
        {
            lockObj = _lockMap.GetOrAdd(key, (key) => new LockObject());
            lockObj.WaitCount++;
        }

        bool acquired = false;
        while (!acquired)
        {
            lock (lockObj)
            {
                if (lockObj.ResetEvent.IsSet)
                {
                    lockObj.ResetEvent.Reset();
                    return;
                }
            }
            if (!acquired) lockObj.ResetEvent.Wait();
        }
    }

    public void MutexAccess(string key, Action action)
    {
        Acquire(key);
        try { action(); }
        finally { Release(key); }
    }

    public async Task MutexAccess(string key, Func<Task> action)
    {
        Acquire(key);
        try { await action(); }
        finally { Release(key); }
    }

    public void Release(string key)
    {
        lock (_lockMap)
        {
            if (_lockMap.TryGetValue(key, out var lockObj))
            {
                lockObj.WaitCount--;
                if (lockObj.WaitCount <= 0)
                {
                    lockObj.Dispose();
                    _lockMap.Remove(key, out _);
                }
                else lockObj.ResetEvent.Set();
            }
        }
    }

    class LockObject : IDisposable
    {
        public LockObject()
        {
            WaitCount = 0;
            ResetEvent = new(initialState: true);
        }

        public int WaitCount { get; set; }
        public ManualResetEventSlim ResetEvent { get; }

        public void Dispose()
        {
            ResetEvent.Dispose();
        }
    }
}