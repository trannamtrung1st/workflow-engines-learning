using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class DynamicRateLimiter : IDynamicRateLimiter, IDisposable
{
    private readonly ManualResetEventSlim _availableEvent;
    private readonly object _lock = new();
    private readonly Queue<long> _queueCounts = new();
    private readonly Queue<long> _availableCounts = new();
    private readonly SemaphoreSlim _rateCollectorLock = new(1);
    private readonly RateLimiterOptions _limiterOptions;
    private System.Timers.Timer _rateCollector;
    private long _limit = 0;
    private long _queueCount = 0;
    private long _acquired = 0;

    public DynamicRateLimiter(RateLimiterOptions limiterOptions)
    {
        _availableEvent = new ManualResetEventSlim();
        _limiterOptions = limiterOptions;
        SetLimit(limit: _limiterOptions.InitialLimit);
    }

    public (long Limit, long Acquired, long Available, long QueueCount) State
    {
        get
        {
            lock (_lock)
            {
                return (_limit, _acquired, _limit - _acquired, _queueCount);
            }
        }
    }

    public IDisposable Acquire(long count)
        => AcquireCore(count, wait: true);

    protected virtual void Release(long count)
    {
        lock (_lock)
        {
            if (_acquired > 0)
            {
                _acquired -= count;
                _availableEvent.Set();
            }
        }
    }

    public long SetLimit(long limit)
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

    public bool TryAcquire(long count, out IDisposable scope)
    {
        scope = AcquireCore(count, wait: false);
        return scope != null;
    }

    protected virtual IDisposable AcquireCore(long count, bool wait)
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
                        Interlocked.Add(ref _queueCount, count);
                        queued = true;
                    }

                    if (CanAcquired())
                    {
                        acquired = true;
                        _acquired += count;
                    }
                    else _availableEvent.Reset();
                }

                if (!acquired)
                {
                    if (wait) _availableEvent.Wait();
                    else return null;
                }
            }
            return new SimpleScope(() => Release(count));
        }
        finally { if (queued) Interlocked.Add(ref _queueCount, -count); }
    }

    protected virtual long GetAcceptedLimit(long limit) => limit;

    protected virtual bool CanAcquired() => _acquired < _limit;

    public void StartRateCollector()
    {
        if (_rateCollector == null)
        {
            var collectorOptions = _limiterOptions.CollectorOptions;
            _rateCollector = new(interval: collectorOptions.Interval);
            _rateCollector.AutoReset = true;
            _rateCollector.Elapsed += async (s, e) =>
            {
                await _rateCollectorLock.WaitAsync();
                try
                {
                    if (_queueCounts.Count == collectorOptions.MovingAverageRange) _queueCounts.TryDequeue(out var _);
                    if (_availableCounts.Count == collectorOptions.MovingAverageRange) _availableCounts.TryDequeue(out var _);
                    var (_, _, available, queueCount) = State;
                    _queueCounts.Enqueue(queueCount);
                    _availableCounts.Enqueue(available);
                }
                finally { _rateCollectorLock.Release(); }
            };
        }
        _rateCollector.Start();
    }

    public void StopRateCollector() => _rateCollector?.Stop();

    public void GetRateStatistics(out long queueCountAvg, out long availableCountAvg)
    {
        _rateCollectorLock.Wait();
        try
        {
            queueCountAvg = _queueCounts.Count > 0 ? (long)_queueCounts.Average() : 0;
            availableCountAvg = _availableCounts.Count > 0 ? (long)_availableCounts.Average() : 0;
        }
        finally { _rateCollectorLock.Release(); }
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _queueCounts?.Clear();
        _availableCounts?.Clear();
        _rateCollectorLock?.Dispose();
        _rateCollector?.Dispose();
        _availableEvent.Dispose();
    }
}