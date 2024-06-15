namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter : IDisposable
{
    (int Limit, int Acquired, int Available, int QueueCount) State { get; }

    int SetLimit(int limit, CancellationToken cancellationToken = default);
    IDisposable Acquire(CancellationToken cancellationToken = default);
    bool TryAcquire(out IDisposable scope, CancellationToken cancellationToken = default);
}