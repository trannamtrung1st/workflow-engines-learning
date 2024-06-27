namespace WELearning.Shared.Concurrency.Abstracts;

public interface IRateScalingController
{
    void Start(IEnumerable<IDynamicRateLimiter> rateLimiters);
    void Stop();
    void StartRateCollector(IEnumerable<IDynamicRateLimiter> rateLimiters);
    void StopRateCollector();
}