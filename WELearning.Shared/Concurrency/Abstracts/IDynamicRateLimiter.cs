namespace WELearning.Shared.Concurrency.Abstracts;

public interface IDynamicRateLimiter
{
    (int Limit, int Acquired, int Available, int QueueCount) State { get; }

    Task SetLimit(int limit, CancellationToken cancellationToken = default);
    Task<IAsyncDisposable> Acquire(CancellationToken cancellationToken = default);
}