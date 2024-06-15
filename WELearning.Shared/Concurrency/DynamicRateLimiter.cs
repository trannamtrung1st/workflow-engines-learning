using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Shared.Concurrency;

public class DynamicRateLimiter : IDynamicRateLimiter, IDisposable
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

    public int SetLimit(int limit, CancellationToken cancellationToken = default)
    {
        var acceptedLimit = GetAcceptedLimit(limit);
        _semaphore.Wait(cancellationToken: cancellationToken);
        try
        {
            var prevLimit = _limit;
            _limit = acceptedLimit;
            if (acceptedLimit > prevLimit) _availableEvent.Set();
        }
        finally { _semaphore.Release(); }
        return acceptedLimit;
    }

    public bool TryAcquire(out IDisposable scope, CancellationToken cancellationToken = default)
    {
        scope = AcquireCore(wait: false, cancellationToken);
        return scope != null;
    }

    public IDisposable AcquireCore(bool wait, CancellationToken cancellationToken = default)
    {
        if (_limit == 0)
            return null;
        bool queued = false;
        bool acquired = false;
        try
        {
            while (!acquired)
            {
                _semaphore.Wait(cancellationToken: cancellationToken);
                if (!queued)
                {
                    Interlocked.Increment(ref _queueCount);
                    queued = true;
                }

                try
                {
                    if (CanAcquired())
                    {
                        acquired = true;
                        _acquired++;
                        Interlocked.Decrement(ref _queueCount);
                        queued = false;
                    }
                    else _availableEvent.Reset();
                }
                finally { _semaphore.Release(); }

                if (!acquired)
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

    protected virtual int GetAcceptedLimit(int limit) => limit;

    protected virtual bool CanAcquired() => _acquired < _limit;

    public void Dispose()
    {
        _semaphore.Dispose();
        _availableEvent.Dispose();
    }
}