using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockWorker : IFunctionBlockWorker
{
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicRateLimiter _dynamicRateLimiter;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private Thread _workerThread;

    public FunctionBlockWorker(
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider,
        IDynamicRateLimiter dynamicRateLimiter,
        ILogger<FunctionBlockWorker> logger)
    {
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
        _dynamicRateLimiter = dynamicRateLimiter;
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
                try
                {
                    var message = _messageQueue.Consume<AttributeChangedEvent>(TopicNames.AttributeChanged);
                    async Task Handle()
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
                        await fbService.HandleAttributeChanged(message, cancellationToken);
                    }

                    if (_dynamicRateLimiter.TryAcquire(out var scope, cancellationToken))
                    {
                        _ = Task.Factory.StartNew(function: async () =>
                        {
                            using var _ = scope;
                            await Handle();
                        }, creationOptions: TaskCreationOptions.LongRunning);
                    }
                    else await Handle();
                }
                catch (Exception ex) { _logger.LogError(ex, ex.Message); }
            }
        });
        _workerThread.IsBackground = true;
        _workerThread.Start();
    }
}