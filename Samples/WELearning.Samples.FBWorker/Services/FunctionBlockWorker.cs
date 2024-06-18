using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Models;
using WELearning.Samples.Shared.RabbitMq.Abstracts;
using WELearning.Shared.Concurrency;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.FBWorker.Services;

public class FunctionBlockWorker : IFunctionBlockWorker, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ILogger<FunctionBlockWorker> _logger;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ConcurrentQueue<WorkerControl> _workers;
    private readonly IConfiguration _configuration;
    private readonly IRabbitMqConnectionManager _rabbitMqConnectionManager;

    public FunctionBlockWorker(
        IServiceProvider serviceProvider,
        ISyncAsyncTaskRunner taskRunner,
        ILogger<FunctionBlockWorker> logger,
        IOptions<AppSettings> appSettings,
        IConfiguration configuration,
        IRabbitMqConnectionManager rabbitMqConnectionManager)
    {
        _serviceProvider = serviceProvider;
        _taskRunner = taskRunner;
        _logger = logger;
        _appSettings = appSettings;
        _configuration = configuration;
        _rabbitMqConnectionManager = rabbitMqConnectionManager;
        _workers = new();
    }

    public void StartWorker(CancellationToken cancellationToken)
    {
        Setup(cancellationToken);
        _rabbitMqConnectionManager.Connect();
        ScaleUp(_appSettings.Value.WorkerCount);
        _appSettings.Value.Changed += HandleWokerChanged;
    }

    private void Setup(CancellationToken cancellationToken)
    {
        CancellationTokenRegistration reg = default;
        reg = cancellationToken.Register(() =>
        {
            using var _ = reg;
            while (_workers.TryDequeue(out var workerControl))
                workerControl.Dispose();
        });
    }

    private WorkerControl CreateControl(string channelId, IModel channel)
    {
        WorkerControl workerControl = null;
        workerControl = new WorkerControl(
            channelScope: new SimpleScope(onDispose: () => _rabbitMqConnectionManager.Close(channelId)),
            channel: channel,
            OnReceived: (s, e) => OnMessageReceived(s, e, workerControl, channel));
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
            var channelId = _workers.Count.ToString();
            _rabbitMqConnectionManager.ConfigureChannel(channelId, SetupRabbitMqChannel(channelId));
            _rabbitMqConnectionManager.Connect(channelId);
            var channel = _rabbitMqConnectionManager.GetChannel(channelId);
            var workerControl = CreateControl(channelId, channel);
            _workers.Enqueue(workerControl);
            workerControl.Start();
        }
    }

    private void ScaleDown(int to)
    {
        while (_workers.Count > to && _workers.TryDequeue(out var workerControl))
            workerControl.Dispose();
    }

    private Action<IModel> SetupRabbitMqChannel(string channelId)
    {
        void ConfigureChannel(IModel channel)
        {
            var rabbitMqChannelOptions = _configuration.GetSection("RabbitMqChannel");
            channel.BasicQos(
                prefetchSize: 0, // RabbitMQ not implemented
                prefetchCount: rabbitMqChannelOptions.GetValue<ushort>("PrefetchCount"),
                global: false);
            channel.ContinuationTimeout = _configuration.GetValue<TimeSpan?>("RabbitMqChannel:ContinuationTimeout") ?? channel.ContinuationTimeout;
            channel.ModelShutdown += (sender, e) => OnModelShutdown(sender, e, channelId);
        }
        return ConfigureChannel;
    }

    private void OnModelShutdown(object sender, ShutdownEventArgs e, string channelId)
    {
        if (e.Exception != null)
            _logger.LogError(e.Exception, "RabbitMQ channel {ChannelId} shutdown reason: {Reason} | Message: {Message}", channelId, e.Cause, e.Exception?.Message);
        else
            _logger.LogInformation("RabbitMQ channel {ChannelId} shutdown reason: {Reason}", channelId, e.Cause);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs e, WorkerControl workerControl, IModel channel)
    {
        CancellationTokenSource cts = new();
        workerControl.Cts = cts;
        AttributeChangedEvent message = JsonSerializer.Deserialize<AttributeChangedEvent>(e.Body.ToArray());

        async Task Handle()
        {
            using var scope = _serviceProvider.CreateScope();
            var fbService = scope.ServiceProvider.GetRequiredService<IFunctionBlockService>();
            await fbService.HandleAttributeChanged(message, cancellationToken: cts.Token);
            channel.BasicAck(e.DeliveryTag, multiple: false);
        }

        await _taskRunner.TryRunTaskAsync(async (asyncScope) =>
        {
            using var _ = asyncScope;
            using var _1 = cts;
            try { await Handle(); }
            catch (Exception ex) { _logger.LogError(ex, ex.Message); }
        }, cancellationToken: cts.Token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _appSettings.Value.Changed -= HandleWokerChanged;
        foreach (var worker in _workers)
            worker.Dispose();
    }

    class WorkerControl(IDisposable channelScope, IModel channel, AsyncEventHandler<BasicDeliverEventArgs> OnReceived) : IDisposable
    {
        private readonly IDisposable _channelScope = channelScope;
        private AsyncEventingBasicConsumer _consumer;
        private string _consumerTag;

        public CancellationTokenSource Cts { get; set; }

        public void Start()
        {
            if (_consumer == null)
            {
                _consumerTag = Guid.NewGuid().ToString();
                _consumer = new AsyncEventingBasicConsumer(channel);
                _consumer.Received += OnReceived;
            }

            channel.BasicConsume(queue: TopicNames.AttributeChanged, autoAck: false, consumerTag: _consumerTag, consumer: _consumer);
        }

        public void Dispose()
        {
            using var _ = _channelScope;
            try { channel.BasicCancel(consumerTag: _consumerTag); } catch { }
            if (_consumer != null)
                _consumer.Received -= OnReceived;
            try { Cts?.Cancel(); } catch { }
            Cts?.Dispose();
        }
    }
}