namespace WELearning.Shared.Concurrency.Abstracts;

public interface IConsumerRateLimiters
{
    ISyncAsyncTaskLimiter TaskLimiter { get; }
    IDynamicRateLimiter SizeLimiter { get; }
    IEnumerable<IDynamicRateLimiter> RateLimiters { get; }
}