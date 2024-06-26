namespace WELearning.Shared.Concurrency.Abstracts;

public interface IRateScalingController
{
    void Start(IDynamicRateLimiter rateLimiter);
    void Stop();
}