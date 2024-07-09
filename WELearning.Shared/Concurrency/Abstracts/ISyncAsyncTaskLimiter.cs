using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency.Abstracts;

public interface ISyncAsyncTaskLimiter : IDynamicRateLimiter
{
    TaskLimiterOptions Options { get; }
}