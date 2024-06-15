using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WELearning.Samples.DeviceService.Configurations;
using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockWorker : IFunctionBlockWorker, IDisposable
{
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ConcurrentQueue<WorkerControl> _workers;
    private CancellationToken _cancellationToken;

    public FunctionBlockWorker(
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider,
        ISyncAsyncTaskRunner taskRunner,
        ILogger<FunctionBlockWorker> logger,
        IOptions<AppSettings> appSettings)
    {
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
        _taskRunner = taskRunner;
        _logger = logger;
        _appSettings = appSettings;
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

        ScaleUp(_appSettings.Value.WorkerCount);

        _appSettings.Value.Changed += HandleWokerChanged;
    }

    private WorkerControl NewWorker()
    {
        // [TODO] apply fuzzy thread controller
        WorkerControl workerControl = null;
        var workerThread = new Thread(async () =>
        {
            using var _ = workerControl;
            while (!_cancellationToken.IsCancellationRequested && !workerControl.Stopped)
            {
                var message = _messageQueue.Consume<AttributeChangedEvent>(TopicNames.AttributeChanged);
                CancellationTokenSource cts = new();
                workerControl.Cts = cts;

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
        });
        workerThread.IsBackground = true;
        workerControl = new WorkerControl(workerThread);
        return workerControl;
    }

    private void HandleWokerChanged(object o, EventArgs e)
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
            workerControl.Stopped = true;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _appSettings.Value.Changed -= HandleWokerChanged;
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