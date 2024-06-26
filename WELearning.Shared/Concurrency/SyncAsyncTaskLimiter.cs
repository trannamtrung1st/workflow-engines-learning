using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskLimiter : DynamicRateLimiter, ISyncAsyncTaskLimiter
{
    private int _asyncCount;
    private readonly int _maxAsyncLimit;

    public SyncAsyncTaskLimiter(
        IOptions<TaskLimiterOptions> limiterOptions,
        ILogger<SyncAsyncTaskLimiter> logger) : base(limiterOptions: limiterOptions.Value)
    {
        // Reference: https://engineering.zalando.com/posts/2019/04/how-to-set-an-ideal-thread-pool-size.html
        var optionsValue = limiterOptions.Value;
        _maxAsyncLimit = (int)(optionsValue.AvailableCores * optionsValue.TargetCpuUtil * (1 + optionsValue.WaitTime / optionsValue.ServiceTime));
        logger.LogDebug("Max async limit: {Limit}", _maxAsyncLimit);
    }

    protected override IDisposable AcquireCore(long count, bool wait)
    {
        var disposable = base.AcquireCore(count, wait);
        if (disposable != null)
            Interlocked.Increment(ref _asyncCount);
        return disposable;
    }

    protected override void Release(long count)
    {
        Interlocked.Decrement(ref _asyncCount);
        base.Release(count);
    }

    protected override bool CanAcquired() => _asyncCount < _maxAsyncLimit && base.CanAcquired();
}