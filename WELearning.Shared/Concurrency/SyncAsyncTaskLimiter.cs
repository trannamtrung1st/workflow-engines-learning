using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskLimiter : DynamicRateLimiter, ISyncAsyncTaskLimiter
{
    private readonly int _maxLimit;

    public SyncAsyncTaskLimiter(IOptions<TaskLimiterOptions> limiterOptions) : base(limiterOptions: limiterOptions.Value)
    {
        // Reference: https://engineering.zalando.com/posts/2019/04/how-to-set-an-ideal-thread-pool-size.html
        var optionsValue = limiterOptions.Value;
        _maxLimit = (int)(optionsValue.AvailableCores * optionsValue.TargetCpuUtil * (1 + optionsValue.WaitTime / optionsValue.ServiceTime));
    }

    protected override int GetAcceptedLimit(int limit) => limit < _maxLimit ? limit : _maxLimit;
}