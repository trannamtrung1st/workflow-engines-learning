using System.Collections.Concurrent;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class InMemoryLockManager : IInMemoryLockManager, IDistributedLockManager
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<string, LockObject> _lockMap;

    public InMemoryLockManager()
    {
        _lockMap = new();
    }

    public ILock Acquire(string key, TimeSpan? expiry = null, TimeSpan? timeout = null, int retries = 3)
    {
        var timeoutCts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        LockObject lockObj;
        lock (_lockMap)
        {
            lockObj = _lockMap.GetOrAdd(key, (key) => new LockObject(key, Release, expiry));
            lockObj.ActiveCount++;
        }

        while (true)
        {
            lock (lockObj)
            {
                if (lockObj.ReadyEvent.IsSet)
                {
                    lockObj.SetAcquired();
                    lockObj.ReadyEvent.Reset();
                    return lockObj;
                }
            }
            lockObj.ReadyEvent.Wait(cancellationToken: timeoutCts.Token);
        }
    }

    public void MutexAccess(string key, Action action)
    {
        using var mutex = Acquire(key);
        action();
    }

    public async Task MutexAccess(string key, Func<Task> action)
    {
        using var mutex = Acquire(key);
        await action();
    }

    private void Release(LockObject lockObj)
    {
        lock (_lockMap)
        {
            lockObj.ActiveCount--;
            if (lockObj.ActiveCount <= 0)
            {
                _lockMap.Remove(lockObj.Key, out _);
                lockObj.HandleLockRemoved();
            }
            else lockObj.SetReady();
        }
    }

    class LockObject : ILock
    {
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromSeconds(30);
        private readonly Func<CancellationTokenSource> _expiryCtsProvider;
        private CancellationTokenSource _currentCts;
        private readonly Action<LockObject> _onRelease;

        public LockObject(string key, Action<LockObject> onRelease, TimeSpan? expiry = null)
        {
            _expiryCtsProvider = () =>
            {
                var cts = new CancellationTokenSource(expiry ?? DefaultExpiry);
                cts.Token.Register(Release);
                return cts;
            };
            _onRelease = onRelease;
            Key = key;
            ActiveCount = 0;
            ReadyEvent = new(initialState: true);
        }

        public string Key { get; }
        public int ActiveCount { get; set; }
        public ManualResetEventSlim ReadyEvent { get; }

        public void Dispose() => Release();

        public void Release() => _onRelease(this);

        public void SetAcquired()
        {
            _currentCts = _expiryCtsProvider();
            ReadyEvent.Reset();
        }

        public void SetReady()
        {
            _currentCts?.Dispose();
            _currentCts = null;
            ReadyEvent.Set();
        }

        public void HandleLockRemoved()
        {
            _currentCts?.Dispose();
            ReadyEvent.Dispose();
        }
    }
}