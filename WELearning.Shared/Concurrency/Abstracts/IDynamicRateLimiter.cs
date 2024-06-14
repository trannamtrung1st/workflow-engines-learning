namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter
{
    (int Limit, int Acquired, int Available, int QueueCount) State { get; }

    void SetLimit(int limit, CancellationToken cancellationToken = default);
    IDisposable Acquire(CancellationToken cancellationToken = default);
    bool TryAcquire(out IDisposable scope, CancellationToken cancellationToken = default);
}