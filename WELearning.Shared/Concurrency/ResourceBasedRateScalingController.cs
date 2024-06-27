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
    private System.Timers.Timer _rateCollector;
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

    public void Start(IEnumerable<IDynamicRateLimiter> rateLimiters)
    {
        if (!_resourceMonitorSet)
        {
            _resourceMonitorSet = true;
            var scalingOptions = _options.Value;
            _resourceMonitor.SetMonitor((cpu, mem) =>
            {
                try
                {
                    _logger.LogInformation("===== Resource consumption =====\nCPU: {Cpu} - Memory: {Memory}", cpu, mem);

                    foreach (var rateLimiter in rateLimiters)
                    {
                        var parameters = scalingOptions.Parameters[rateLimiter.Name];
                        ScaleRate(rateLimiter, cpu, mem, parameters);
                    }
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

    private void ScaleRate(IDynamicRateLimiter rateLimiter, double cpu, double mem, ScalingParameters parameters)
    {
        int scaleFactor = parameters.ScaleFactor;
        if (_lastCpu > 0 && _lastMem > 0)
        {
            var cpuDiff = Math.Abs(cpu - _lastCpu);
            var memDiff = Math.Abs(mem - _lastMem);
            var biggerDiff = Math.Max(cpuDiff, memDiff);
            scaleFactor -= (int)(scaleFactor * biggerDiff);
        }
        _lastCpu = cpu; _lastMem = mem;
        var rateScale = _fuzzyRateScaler.GetRateScale(cpu, mem, ideal: parameters.IdealUsage, factor: scaleFactor);
        if (rateScale == 0) return;
        var (rateLimit, acquired, _, _) = rateLimiter.State;
        rateLimiter.GetRateStatistics(out var availableCountAvg, out var queueCountAvg);
        int newLimit;
        if (rateScale < 0)
            newLimit = rateLimit + rateScale;
        else if (availableCountAvg > parameters.AcceptedAvailablePercentage * rateLimit && queueCountAvg <= parameters.AcceptedQueueCount)
            newLimit = rateLimit - rateScale / 2;
        else
            newLimit = rateLimit + rateScale;
        if (newLimit < rateLimiter.InitialLimit) newLimit = rateLimiter.InitialLimit;
        rateLimiter.SetLimit(newLimit);
        _logger.LogInformation(
            "Limiter: {Limiter}\n" +
            "Scale: {Scale} - Acquired: {Acquired} - Available: {Available} - Queue: {QueueCount}\n" +
            "New rate limit: {Limit}",
            rateLimiter.Name, rateScale, acquired, availableCountAvg, queueCountAvg, newLimit);
    }

    public void StartRateCollector(IEnumerable<IDynamicRateLimiter> rateLimiters)
    {
        if (_rateCollector == null)
        {
            var collectorOptions = _options.Value.RateCollectorOptions;
            _rateCollector = new(interval: collectorOptions.Interval);
            _rateCollector.AutoReset = true;
            _rateCollector.Elapsed += (s, e) =>
            {
                foreach (var limiter in rateLimiters)
                    limiter.CollectRate(collectorOptions.MovingAverageRange);
            };
        }
        _rateCollector.Start();
    }

    public void StopRateCollector() => _rateCollector?.Stop();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _resourceMonitor?.Stop();
        _rateCollector?.Dispose();
    }
}