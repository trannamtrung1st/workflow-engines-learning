using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Shared.Concurrency;

public class ResourceBasedRateScalingController : IRateScalingController, IDisposable
{
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IResourceBasedFuzzyRateScaler _fuzzyRateScaler;
    private readonly ILogger<ResourceBasedRateScalingController> _logger;
    private readonly IOptions<ResourceBasedRateScalingOptions> _options;
    private bool _resourceMonitorSet = false;
    private double _lastCpu;
    private double _lastMem;

    public ResourceBasedRateScalingController(
        IResourceBasedFuzzyRateScaler fuzzyRateScaler,
        IResourceMonitor resourceMonitor,
        ILogger<ResourceBasedRateScalingController> logger,
        IOptions<ResourceBasedRateScalingOptions> options)
    {
        _fuzzyRateScaler = fuzzyRateScaler;
        _resourceMonitor = resourceMonitor;
        _logger = logger;
        _options = options;
    }

    public void Start(IDynamicRateLimiter rateLimiter)
    {
        if (!_resourceMonitorSet)
        {
            _resourceMonitorSet = true;
            var scalingOptions = _options.Value;
            _resourceMonitor.SetMonitor((cpu, mem) =>
            {
                try
                {
                    ScaleRate(rateLimiter, cpu, mem,
                        ideal: scalingOptions.IdealUsage, scaleFactor: scalingOptions.ScaleFactor,
                        initialLimit: scalingOptions.InitialLimit,
                        acceptedQueueCount: scalingOptions.AcceptedQueueCount,
                        acceptedAvailablePercentage: scalingOptions.AcceptedAvailablePercentage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return Task.CompletedTask;
            }, interval: scalingOptions.ScaleCheckInterval);
        }

        _resourceMonitor.Start();
    }

    public void Stop() => _resourceMonitor?.Stop();

    private void ScaleRate(IDynamicRateLimiter rateLimiter, double cpu, double mem, double ideal,
        int scaleFactor, int initialLimit, int acceptedQueueCount, double acceptedAvailablePercentage)
    {
        if (_lastCpu > 0 && _lastMem > 0)
        {
            var cpuDiff = Math.Abs(cpu - _lastCpu);
            var memDiff = Math.Abs(mem - _lastMem);
            var biggerDiff = Math.Max(cpuDiff, memDiff);
            scaleFactor -= (int)(scaleFactor * biggerDiff);
        }
        _lastCpu = cpu; _lastMem = mem;
        var rateScale = _fuzzyRateScaler.GetRateScale(cpu, mem, ideal, factor: scaleFactor);
        if (rateScale == 0) return;
        var (rateLimit, acquired, availableCountAvg, queueCountAvg) = rateLimiter.State;
        long newLimit;
        if (rateScale < 0)
            newLimit = rateLimit + rateScale;
        else if (availableCountAvg > acceptedAvailablePercentage * rateLimit && queueCountAvg <= acceptedQueueCount)
            newLimit = rateLimit - rateScale / 2;
        else
            newLimit = rateLimit + rateScale;
        if (newLimit < initialLimit) newLimit = initialLimit;
        rateLimiter.SetLimit(newLimit);
        _logger.LogWarning(
            "CPU: {Cpu} - Memory: {Memory}\n" +
            "Scale: {Scale} - Acquired: {Acquired} - Available: {Available} - Queue: {QueueCount}\n" +
            "New rate limit: {Limit}",
            cpu, mem, rateScale, acquired, availableCountAvg, queueCountAvg, newLimit);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _resourceMonitor?.Stop();
    }
}