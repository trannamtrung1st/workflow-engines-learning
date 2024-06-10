using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockWorker : IFunctionBlockWorker
{
    private readonly IConfiguration _configuration;
    private readonly List<Thread> _workerThreads;
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;

    public FunctionBlockWorker(
        IConfiguration configuration,
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider)
    {
        _workerThreads = new();
        _configuration = configuration;
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
    }

    public void StartWorkers(CancellationToken cancellationToken)
    {
        var workerCount = _configuration.GetValue<int>("FunctionBlock:WorkerCount");
        for (int i = 0; i < workerCount; i++)
        {
            var thread = new Thread(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = _messageQueue.Consume<AttributeChangedEvent>(TopicNames.AttributeChanged);
                    using var scope = _serviceProvider.CreateScope();
                    var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
                    await fbService.HandleAttributeChanged(message, cancellationToken);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            _workerThreads.Add(thread);
        }
    }
}