using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class DynamicRateLimiter : IDynamicRateLimiter, IDisposable
{
    private readonly ManualResetEventSlim _availableEvent;
    private readonly SemaphoreSlim _semaphore;
    private readonly Queue<int> _queueCounts = new();
    private readonly Queue<int> _availableCounts = new();
    private readonly SemaphoreSlim _concurrencyCollectorLock = new(1);
    private readonly RateLimiterOptions _limiterOptions;
    private System.Timers.Timer _concurrencyCollector;
    private int _limit = 0;
    private int _queueCount = 0;
    private int _acquired = 0;

    public DynamicRateLimiter(RateLimiterOptions limiterOptions)
    {
        _semaphore = new SemaphoreSlim(1);
        _availableEvent = new ManualResetEventSlim();
        _limiterOptions = limiterOptions;
        SetLimit(limit: _limiterOptions.InitialLimit);
    }

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
        finally { if (queued) Interlocked.Decrement(ref _queueCount); }
    }

    protected virtual int GetAcceptedLimit(int limit) => limit;

    protected virtual bool CanAcquired() => _acquired < _limit;

    public void StartConcurrencyCollector()
    {
        if (_concurrencyCollector == null)
        {
            var collectorOptions = _limiterOptions.CollectorOptions;
            _concurrencyCollector = new(interval: collectorOptions.ConcurrencyCollectorInterval);
            _concurrencyCollector.AutoReset = true;
            _concurrencyCollector.Elapsed += async (s, e) =>
            {
                await _concurrencyCollectorLock.WaitAsync();
                try
                {
                    if (_queueCounts.Count == collectorOptions.MovingAverageRange) _queueCounts.TryDequeue(out var _);
                    if (_availableCounts.Count == collectorOptions.MovingAverageRange) _availableCounts.TryDequeue(out var _);
                    var (_, _, concurrencyAvailable, concurrencyQueueCount) = State;
                    _queueCounts.Enqueue(concurrencyQueueCount);
                    _availableCounts.Enqueue(concurrencyAvailable);
                }
                finally { _concurrencyCollectorLock.Release(); }
            };
        }
        _concurrencyCollector.Start();
    }

    public void StopConcurrencyCollector() => _concurrencyCollector?.Stop();

    public void GetConcurrencyStatistics(out int queueCountAvg, out int availableCountAvg)
    {
        _concurrencyCollectorLock.Wait();
        try
        {
            queueCountAvg = _queueCounts.Count > 0 ? (int)_queueCounts.Average() : 0;
            availableCountAvg = _availableCounts.Count > 0 ? (int)_availableCounts.Average() : 0;
        }
        finally { _concurrencyCollectorLock.Release(); }
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _queueCounts?.Clear();
        _availableCounts?.Clear();
        _concurrencyCollectorLock?.Dispose();
        _concurrencyCollector?.Dispose();
        _semaphore.Dispose();
        _availableEvent.Dispose();
    }
}