namespace WELearning.Shared.Concurrency.Configurations;

// Reference: https://engineering.zalando.com/posts/2019/04/how-to-set-an-ideal-thread-pool-size.html
public class TaskLimiterOptions : RateLimiterOptions
{
    public double AvailableCores { get; set; }
    public double TargetCpuUtil { get; set; }
    public double WaitTime { get; set; }
    public double ServiceTime { get; set; }
    public int AcquireRatePerSecond { get; set; } = 5;
    public new int InitialLimit { get; set; }
    public new RateCollectorOptions CollectorOptions { get; set; }
}
