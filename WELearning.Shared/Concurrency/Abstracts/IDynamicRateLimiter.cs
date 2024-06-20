namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter : IDisposable
{
    (int Limit, int Acquired, int Available, int QueueCount) State { get; }

    int SetLimit(int limit);
    IDisposable Acquire();
    bool TryAcquire(out IDisposable scope);
    void StartRateCollector();
    void StopRateCollector();
    void GetRateStatistics(out int queueCountAvg, out int availableCountAvg);
}