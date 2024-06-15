using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskLimiter : DynamicRateLimiter, ISyncAsyncTaskLimiter
{
    private readonly int _maxLimit;
    public SyncAsyncTaskLimiter(IOptions<TaskLimiterOptions> options)
    {
        // Reference: https://engineering.zalando.com/posts/2019/04/how-to-set-an-ideal-thread-pool-size.html
        var limiterOptions = options.Value;
        _maxLimit = (int)(limiterOptions.AvailableCores * limiterOptions.TargetCpuUtil * (1 + limiterOptions.WaitTime / limiterOptions.ServiceTime));
        SetLimit(limit: limiterOptions.InitialLimit);
    }

    protected override int GetAcceptedLimit(int limit) => limit < _maxLimit ? limit : _maxLimit;
}