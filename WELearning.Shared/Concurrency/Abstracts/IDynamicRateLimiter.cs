namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter : IDisposable
{
    (long Limit, long Acquired, long Available, long QueueCount) State { get; }

    long SetLimit(long limit);
    IDisposable Acquire(long count);
    bool TryAcquire(long count, out IDisposable scope);
    void StartRateCollector();
    void StopRateCollector();
    void GetRateStatistics(out long queueCountAvg, out long availableCountAvg);
}