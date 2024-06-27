using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;
using Microsoft.Extensions.Logging;

namespace WELearning.Shared.Concurrency;

public class ConsumerRateLimiters : IConsumerRateLimiters
{
    public ISyncAsyncTaskLimiter TaskLimiter { get; }
    public IDynamicRateLimiter SizeLimiter { get; }
    public IEnumerable<IDynamicRateLimiter> RateLimiters { get; }

    public ConsumerRateLimiters(
        TaskLimiterOptions taskLimiterOptions,
        RateLimiterOptions sizeLimiterOptions,
        ILogger<SyncAsyncTaskLimiter> taskLogger)
    {
        var rateLimiters = new List<IDynamicRateLimiter>();

        if (taskLimiterOptions != null)
            rateLimiters.Add(TaskLimiter = new SyncAsyncTaskLimiter(taskLimiterOptions, taskLogger));

        if (sizeLimiterOptions != null)
            rateLimiters.Add(SizeLimiter = new DynamicRateLimiter(sizeLimiterOptions));

        RateLimiters = rateLimiters;
    }
}