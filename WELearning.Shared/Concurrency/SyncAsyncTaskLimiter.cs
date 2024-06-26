using System.Collections.Concurrent;
using System.Timers;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

namespace WELearning.Shared.Concurrency;

public class SyncAsyncTaskLimiter : DynamicRateLimiter, ISyncAsyncTaskLimiter
{
    private const int DefaultCleanUpInterval = 3000;
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    private int _asyncCount;
    private readonly int _maxAsyncLimit;
    private readonly int _acquireRatePerSecond;
    private readonly System.Timers.Timer _cleanUpTimer;
    private readonly ConcurrentQueue<DateTime> _acquiredQueue;

    public SyncAsyncTaskLimiter(IOptions<TaskLimiterOptions> limiterOptions) : base(limiterOptions: limiterOptions.Value)
    {
        _acquiredQueue = new();

        // Reference: https://engineering.zalando.com/posts/2019/04/how-to-set-an-ideal-thread-pool-size.html
        var optionsValue = limiterOptions.Value;
        _maxAsyncLimit = (int)(optionsValue.AvailableCores * optionsValue.TargetCpuUtil * (1 + optionsValue.WaitTime / optionsValue.ServiceTime));
        _acquireRatePerSecond = optionsValue.AcquireRatePerSecond;

        _cleanUpTimer = new System.Timers.Timer(interval: DefaultCleanUpInterval);
        _cleanUpTimer.Elapsed += CleanUp;
        _cleanUpTimer.AutoReset = true;
        _cleanUpTimer.Start();
    }

    protected override IDisposable AcquireCore(long count, bool wait)
    {
        var disposable = base.AcquireCore(count, wait);
        if (disposable != null)
        {
            _acquiredQueue.Enqueue(DateTime.UtcNow);
            Interlocked.Increment(ref _asyncCount);
        }
        return disposable;
    }

    protected override void Release(long count)
    {
        Interlocked.Decrement(ref _asyncCount);
        base.Release(count);
    }

    protected override bool CanAcquired() =>
        _asyncCount < _maxAsyncLimit
        && AcquireRatePerSecond() < _acquireRatePerSecond
        && base.CanAcquired();

    private int AcquireRatePerSecond() => _acquiredQueue.Where(WithinOneSecond).Count();

    private void CleanUp(object sender, ElapsedEventArgs e)
    {
        while (_acquiredQueue.TryDequeue(out var acquiredTime) && !WithinOneSecond(acquiredTime)) ;
    }

    private bool WithinOneSecond(DateTime dateTime) => DateTime.UtcNow - dateTime < OneSecond;

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        base.Dispose();
        _cleanUpTimer.Elapsed -= CleanUp;
        _cleanUpTimer.Dispose();
    }
}