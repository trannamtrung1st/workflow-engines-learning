using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockWorker : IFunctionBlockWorker
{
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private Thread _workerThread;

    public FunctionBlockWorker(
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider,
        ISyncAsyncTaskRunner taskRunner,
        ILogger<FunctionBlockWorker> logger)
    {
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
        _taskRunner = taskRunner;
        _logger = logger;
    }

    public void StartWorker(CancellationToken cancellationToken)
    {
        // [TODO] add manual control
        // [TODO] apply fuzzy thread controller
        _workerThread = new Thread(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = _messageQueue.Consume<AttributeChangedEvent>(TopicNames.AttributeChanged);
                async Task Handle()
                {
                    using var scope = _serviceProvider.CreateScope();
                    var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
                    await fbService.HandleAttributeChanged(message, cancellationToken);
                }

                await _taskRunner.TryRunTaskAsync(async (asyncScope) =>
                {
                    using var _2 = asyncScope;
                    try { await Handle(); }
                    catch (Exception ex) { _logger.LogError(ex, ex.Message); }
                }, cancellationToken);
            }
        });
        _workerThread.IsBackground = true;
        _workerThread.Start();
    }
}