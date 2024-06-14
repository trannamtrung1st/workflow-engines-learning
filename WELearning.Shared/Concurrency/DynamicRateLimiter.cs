using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class DynamicRateLimiter : IDynamicRateLimiter
{
    private readonly ManualResetEventSlim _availableEvent;
    private readonly SemaphoreSlim _semaphore;

    private int _limit = 0;
    private int _queueCount = 0;
    private int _acquired = 0;

    public (int Limit, int Acquired, int Available, int QueueCount) State
    {
        get
        {
            try
            {
                _semaphore.Wait();
                return (_limit, _acquired, _limit - _acquired, _queueCount);
            }
            finally { _semaphore.Release(); }
        }
    }

    public DynamicRateLimiter()
    {
        _semaphore = new SemaphoreSlim(1);
        _availableEvent = new ManualResetEventSlim();
    }

    public IDisposable Acquire(CancellationToken cancellationToken = default)
        => AcquireCore(wait: true, cancellationToken);

    private void Release(CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken: cancellationToken);
        try
        {
            if (_acquired > 0)
            {
                _acquired--;
                _availableEvent.Set();
            }
        }
        finally { _semaphore.Release(); }
    }

    public void SetLimit(int limit, CancellationToken cancellationToken = default)
    {
        _semaphore.Wait(cancellationToken: cancellationToken);
        try
        {
            var prevLimit = _limit;
            _limit = limit;
            if (limit > prevLimit) _availableEvent.Set();
        }
        finally { _semaphore.Release(); }
    }

    public bool TryAcquire(out IDisposable scope, CancellationToken cancellationToken = default)
    {
        scope = AcquireCore(wait: false, cancellationToken);
        return scope != null;
    }

    public IDisposable AcquireCore(bool wait, CancellationToken cancellationToken = default)
    {
        bool queued = false;
        bool canAcquired = false;
        try
        {
            while (!canAcquired)
            {
                _semaphore.Wait(cancellationToken: cancellationToken);
                if (!queued)
                {
                    Interlocked.Increment(ref _queueCount);
                    queued = true;
                }

                try
                {
                    if (_acquired < _limit)
                    {
                        canAcquired = true;
                        _acquired++;
                        Interlocked.Decrement(ref _queueCount);
                        queued = false;
                    }
                    else _availableEvent.Reset();
                }
                finally { _semaphore.Release(); }

                if (!canAcquired)
                {
                    if (wait)
                        _availableEvent.Wait(cancellationToken);
                    else return null;
                }
            }
            return new SimpleScope(() => Release(cancellationToken));
        }
        catch
        {
            if (queued) Interlocked.Decrement(ref _queueCount);
            throw;
        }
    }
}