namespace WELearning.Shared.Concurrency.Abstracts;

public interface IConcurrencyScalingController
{
    void Start(IDynamicRateLimiter concurrencyLimiter);
    void Stop();
}