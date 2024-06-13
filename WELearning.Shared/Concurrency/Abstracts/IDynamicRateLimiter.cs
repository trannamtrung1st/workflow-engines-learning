namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter
{
    (int Limit, int Acquired, int Available, int QueueCount) State { get; }

    Task SetLimit(int limit, CancellationToken cancellationToken = default);
    Task<IDisposable> Acquire(CancellationToken cancellationToken = default);
    bool TryAcquire(out IDisposable scope, CancellationToken cancellationToken = default);
}