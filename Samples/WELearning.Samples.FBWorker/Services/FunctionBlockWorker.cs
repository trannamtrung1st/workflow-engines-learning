using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Models;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Samples.FBWorker.Services;

public class FunctionBlockWorker : IFunctionBlockWorker, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ISyncAsyncTaskLimiter _taskLimiter;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ConcurrentQueue<WorkerControl> _workers;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IConfiguration _configuration;
    private readonly Queue<int> _queueCounts = new Queue<int>();
    private readonly Queue<int> _availableCounts = new Queue<int>();
    private readonly SemaphoreSlim _concurrencyCollectorLock = new SemaphoreSlim(1);
    private readonly IFuzzyThreadController _fuzzyThreadController;
    private bool _resourceMonitorSet = false;
    private System.Timers.Timer _concurrencyCollector;
    private CancellationToken _cancellationToken;

    public FunctionBlockWorker(
        IServiceProvider serviceProvider,
        ISyncAsyncTaskRunner taskRunner,
        ILogger<FunctionBlockWorker> logger,
        IOptions<AppSettings> appSettings,
        IConfiguration configuration,
        IFuzzyThreadController fuzzyThreadController,
        ISyncAsyncTaskLimiter taskLimiter,
        IResourceMonitor resourceMonitor)
    {
        _serviceProvider = serviceProvider;
        _taskRunner = taskRunner;
        _taskLimiter = taskLimiter;
        _logger = logger;
        _appSettings = appSettings;
        _configuration = configuration;
        _fuzzyThreadController = fuzzyThreadController;
        _resourceMonitor = resourceMonitor;
        _workers = new();
    }

    public void StartWorker(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        CancellationTokenRegistration reg = default;
        reg = cancellationToken.Register(() =>
        {
            using var _ = reg;
            foreach (var worker in _workers)
                worker.Cts?.Cancel();
        });

        StartConcurrencyCollector();
        ScaleUp(_appSettings.Value.WorkerCount);

        _appSettings.Value.Changed += HandleWokerChanged;
    }

    public void StartDynamicScalingWorker()
    {
        if (!_resourceMonitorSet)
        {
            _resourceMonitorSet = true;
            var scaleFactor = _configuration.GetValue<int>("AppSettings:ScaleFactor");
            var acceptedQueueCount = _configuration.GetValue<int>("AppSettings:AcceptedQueueCount");
            var acceptedAvailableConcurrency = _configuration.GetValue<double>("AppSettings:AcceptedAvailableConcurrency");
            var idealUsage = _configuration.GetValue<double>("AppSettings:IdealUsage");
            _resourceMonitor.SetMonitor(async (cpu, mem) =>
            {
                try
                {
                    await ScaleConcurrency(
                        cpu, mem, ideal: idealUsage, scaleFactor,
                        initialConcurrencyLimit: _appSettings.Value.InitialConcurrencyLimit,
                        acceptedQueueCount, acceptedAvailableConcurrency);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }, interval: _configuration.GetValue<int>("AppSettings:ScaleCheckInterval"));
        }
        _resourceMonitor.Start();
    }

    public void StopDynamicScalingWorker() => _resourceMonitor?.Stop();

    private async Task ScaleConcurrency(double cpu, double mem, double ideal, int scaleFactor, int initialConcurrencyLimit, int acceptedQueueCount, double acceptedAvailableConcurrency)
    {
        var threadScale = _fuzzyThreadController.GetThreadScale(cpu, mem, ideal, factor: scaleFactor);
        if (threadScale == 0) return;
        var (queueCountAvg, availableCountAvg) = await GetConcurrencyStatistics();
        var (concurrencyLimit, _, _, _) = _taskLimiter.State;
        int newLimit;
        if (threadScale < 0)
            newLimit = concurrencyLimit + threadScale;
        else if (queueCountAvg <= acceptedQueueCount && availableCountAvg > acceptedAvailableConcurrency * concurrencyLimit)
            newLimit = concurrencyLimit - threadScale / 2;
        else
            newLimit = concurrencyLimit + threadScale;
        if (newLimit < initialConcurrencyLimit) newLimit = initialConcurrencyLimit;
        _taskLimiter.SetLimit(newLimit);
        _logger.LogWarning(
            "CPU: {Cpu} - Memory: {Memory}\n" +
            "Scale: {Scale} - Available count: {Available} - Queue count: {QueueCount}\n" +
            "New thread limit: {Limit}",
            cpu, mem, threadScale, availableCountAvg, queueCountAvg, newLimit);
    }

    private void StartConcurrencyCollector()
    {
        if (_concurrencyCollector == null)
        {
            var movingAvgRange = _configuration.GetValue<int>("AppSettings:MovingAverageRange");
            var collectorInterval = _configuration.GetValue<int>("AppSettings:ConcurrencyCollectorInterval");
            _concurrencyCollector = new System.Timers.Timer(collectorInterval);
            _concurrencyCollector.AutoReset = true;
            _concurrencyCollector.Elapsed += async (s, e) =>
            {
                await _concurrencyCollectorLock.WaitAsync();
                try
                {
                    if (_queueCounts.Count == movingAvgRange) _queueCounts.TryDequeue(out var _);
                    if (_availableCounts.Count == movingAvgRange) _availableCounts.TryDequeue(out var _);
                    var (_, _, concurrencyAvailable, concurrencyQueueCount) = _taskLimiter.State;
                    _queueCounts.Enqueue(concurrencyQueueCount);
                    _availableCounts.Enqueue(concurrencyAvailable);
                }
                finally { _concurrencyCollectorLock.Release(); }
            };
        }
        _concurrencyCollector.Start();
    }

    private async Task<(int QueueCountAvg, int AvailableCountAvg)> GetConcurrencyStatistics()
    {
        int queueCountAvg;
        int availableCountAvg;
        await _concurrencyCollectorLock.WaitAsync();
        try
        {
            queueCountAvg = _queueCounts.Count > 0 ? (int)_queueCounts.Average() : 0;
            availableCountAvg = _availableCounts.Count > 0 ? (int)_availableCounts.Average() : 0;
            return (queueCountAvg, availableCountAvg);
        }
        finally { _concurrencyCollectorLock.Release(); }
    }

    // [TODO] mem leak
    private WorkerControl NewWorker()
    {
        WorkerControl workerControl = null;
        var workerThread = new Thread(async () =>
        {
            using var _ = workerControl;

            try
            {
                while (!_cancellationToken.IsCancellationRequested && !workerControl.Stopped)
                {
                    CancellationTokenSource cts = new();
                    workerControl.Cts = cts;
                    AttributeChangedEvent message = default; // [TODO]

                    async Task Handle()
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
                        await fbService.HandleAttributeChanged(message, cancellationToken: cts.Token);
                    }

                    await _taskRunner.TryRunTaskAsync(async (asyncScope) =>
                    {
                        using var _ = asyncScope;
                        using var _1 = cts;
                        try { await Handle(); }
                        catch (Exception ex) { _logger.LogError(ex, ex.Message); }
                    }, cancellationToken: cts.Token);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        });
        workerThread.IsBackground = true;
        workerControl = new WorkerControl(workerThread);
        return workerControl;
    }

    private void HandleWokerChanged(object o, IEnumerable<string> changes)
    {
        var currentCount = _workers.Count;
        var newWorkerCount = _appSettings.Value.WorkerCount;
        if (currentCount == newWorkerCount) return;
        if (currentCount > newWorkerCount)
            ScaleDown(newWorkerCount);
        else
            ScaleUp(newWorkerCount);
    }

    private void ScaleUp(int to)
    {
        while (_workers.Count < to)
        {
            var workerControl = NewWorker();
            _workers.Enqueue(workerControl);
            workerControl.Start();
        }
    }

    private void ScaleDown(int to)
    {
        while (_workers.Count > to && _workers.TryDequeue(out var workerControl))
        {
            workerControl.Stopped = true;
            workerControl.Cts?.Cancel();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _appSettings.Value.Changed -= HandleWokerChanged;
        _concurrencyCollectorLock?.Dispose();
        _queueCounts?.Clear();
        _availableCounts?.Clear();
        _resourceMonitor?.Stop();
        _concurrencyCollector?.Dispose();
        foreach (var worker in _workers)
            worker.Dispose();
    }

    class WorkerControl : IDisposable
    {
        public WorkerControl(Thread thread)
        {
            Thread = thread;
            Stopped = false;
        }

        public CancellationTokenSource Cts { get; set; }
        public Thread Thread { get; }
        public bool Stopped { get; set; }

        public void Start() => Thread.Start();

        public void Dispose()
        {
            Cts?.Dispose();
        }
    }
}