using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Shared.Concurrency;

public class ResourceBasedConcurrencyScalingController : IConcurrencyScalingController, IDisposable
{
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IFuzzyThreadController _fuzzyThreadController;
    private readonly ILogger<ResourceBasedConcurrencyScalingController> _logger;
    private readonly IOptions<ResourceBasedConcurrencyScalingOptions> _options;
    private bool _resourceMonitorSet = false;
    private double _lastCpu;
    private double _lastMem;

    public ResourceBasedConcurrencyScalingController(
        IFuzzyThreadController fuzzyThreadController,
        IResourceMonitor resourceMonitor,
        ILogger<ResourceBasedConcurrencyScalingController> logger,
        IOptions<ResourceBasedConcurrencyScalingOptions> options)
    {
        _fuzzyThreadController = fuzzyThreadController;
        _resourceMonitor = resourceMonitor;
        _logger = logger;
        _options = options;
    }

    public void Start(IDynamicRateLimiter concurrencyLimiter)
    {
        if (!_resourceMonitorSet)
        {
            _resourceMonitorSet = true;
            var scalingOptions = _options.Value;
            _resourceMonitor.SetMonitor((cpu, mem) =>
            {
                try
                {
                    ScaleConcurrency(concurrencyLimiter, cpu, mem,
                        ideal: scalingOptions.IdealUsage, scaleFactor: scalingOptions.ScaleFactor,
                        initialConcurrencyLimit: scalingOptions.InitialConcurrencyLimit,
                        acceptedQueueCount: scalingOptions.AcceptedQueueCount,
                        acceptedAvailableConcurrency: scalingOptions.AcceptedAvailableConcurrency);
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

    private void ScaleConcurrency(IDynamicRateLimiter concurrencyLimiter, double cpu, double mem, double ideal,
        int scaleFactor, int initialConcurrencyLimit, int acceptedQueueCount, double acceptedAvailableConcurrency)
    {
        if (_lastCpu > 0 && _lastMem > 0)
        {
            var cpuDiff = Math.Abs(cpu - _lastCpu);
            var memDiff = Math.Abs(mem - _lastMem);
            var biggerDiff = Math.Max(cpuDiff, memDiff);
            scaleFactor -= (int)(scaleFactor * biggerDiff);
        }
        _lastCpu = cpu; _lastMem = mem;
        var threadScale = _fuzzyThreadController.GetThreadScale(cpu, mem, ideal, factor: scaleFactor);
        if (threadScale == 0) return;
        var (concurrencyLimit, _, availableCountAvg, queueCountAvg) = concurrencyLimiter.State;
        int newLimit;
        if (threadScale < 0)
            newLimit = concurrencyLimit + threadScale;
        else if (availableCountAvg > acceptedAvailableConcurrency * concurrencyLimit && queueCountAvg <= acceptedQueueCount)
            newLimit = concurrencyLimit - threadScale / 2;
        else
            newLimit = concurrencyLimit + threadScale;
        if (newLimit < initialConcurrencyLimit) newLimit = initialConcurrencyLimit;
        concurrencyLimiter.SetLimit(newLimit);
        _logger.LogWarning(
            "CPU: {Cpu} - Memory: {Memory}\n" +
            "Scale: {Scale} - Available count: {Available} - Queue count: {QueueCount}\n" +
            "New thread limit: {Limit}",
            cpu, mem, threadScale, availableCountAvg, queueCountAvg, newLimit);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _resourceMonitor?.Stop();
    }
}