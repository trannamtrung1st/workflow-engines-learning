using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockWorker : IFunctionBlockWorker
{
    private readonly IConfiguration _configuration;
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDynamicRateLimiter _dynamicRateLimiter;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private readonly List<Thread> _workerThreads;

    public FunctionBlockWorker(
        IConfiguration configuration,
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider,
        IDynamicRateLimiter dynamicRateLimiter,
        ILogger<FunctionBlockWorker> logger)
    {
        _configuration = configuration;
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
        _dynamicRateLimiter = dynamicRateLimiter;
        _dynamicRateLimiter.SetLimit(_configuration.GetValue<int>("AppSettings:InitialConcurrencyLimit")).Wait();
        _logger = logger;
        _workerThreads = new();
    }

    public void StartWorker(CancellationToken cancellationToken)
    {
        var (concurrencyLimit, _, _, _) = _dynamicRateLimiter.State;
        for (int i = 0; i < concurrencyLimit; i++)
        {
            // [TODO] apply fuzzy thread controller
            var worker = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var message = _messageQueue.Consume<AttributeChangedEvent>(TopicNames.AttributeChanged);
                        using var scope = _serviceProvider.CreateScope();
                        var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
                        await fbService.HandleAttributeChanged(message, cancellationToken);
                    }
                    catch (Exception ex) { _logger.LogError(ex, ex.Message); }
                }
            });
            worker.IsBackground = true;
            worker.Start();
            _workerThreads.Add(worker);
        }
    }
}