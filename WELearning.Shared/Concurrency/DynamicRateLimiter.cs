using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class DynamicRateLimiter : IDynamicRateLimiter, IDisposable
{
    private readonly ManualResetEventSlim _availableEvent;
    private readonly object _lock = new();
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
        _availableEvent = new ManualResetEventSlim();
        _limiterOptions = limiterOptions;
        SetLimit(limit: _limiterOptions.InitialLimit);
    }

    public (int Limit, int Acquired, int Available, int QueueCount) State
    {
        get
        {
            lock (_lock)
            {
                return (_limit, _acquired, _limit - _acquired, _queueCount);
            }
        }
    }

    public IDisposable Acquire()
        => AcquireCore(wait: true);

    private void Release()
    {
        lock (_lock)
        {
            if (_acquired > 0)
            {
                _acquired--;
                _availableEvent.Set();
            }
        }
    }

    public int SetLimit(int limit)
    {
        var acceptedLimit = GetAcceptedLimit(limit);
        lock (_lock)
        {
            var prevLimit = _limit;
            _limit = acceptedLimit;
            if (_limit > prevLimit) _availableEvent.Set();
        }
        return acceptedLimit;
    }

    public bool TryAcquire(out IDisposable scope)
    {
        scope = AcquireCore(wait: false);
        return scope != null;
    }

    public IDisposable AcquireCore(bool wait)
    {
        if (_limit == 0)
            return null;
        bool queued = false;
        bool acquired = false;
        try
        {
            while (!acquired)
            {
                lock (_lock)
                {
                    if (!queued)
                    {
                        Interlocked.Increment(ref _queueCount);
                        queued = true;
                    }

                    if (CanAcquired())
                    {
                        acquired = true;
                        _acquired++;
                    }
                    else _availableEvent.Reset();
                }

                if (!acquired)
                {
                    if (wait) _availableEvent.Wait();
                    else return null;
                }
            }
            return new SimpleScope(Release);
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
        _availableEvent.Dispose();
    }
}