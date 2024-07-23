
using WELearning.Samples.FBWorker;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Samples.FBWorker.FunctionBlock;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Extensions;
using WELearning.Core.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using TNT.Boilerplates.Concurrency.Abstracts;
using TNT.Boilerplates.Diagnostic.Abstracts;
using TNT.Boilerplates.Diagnostic.Extensions;
using TNT.Boilerplates.Concurrency.Extensions;
using WELearning.Samples.FBWorker.Constants;
using TNT.Boilerplates.Concurrency.Configurations;
using TNT.Boilerplates.Concurrency;

const int minThreads = 512;
int maxThreads = minThreads * 2;
var threadSet = ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
threadSet = ThreadPool.SetMaxThreads(workerThreads: maxThreads, completionPortThreads: maxThreads);

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var rateScalingConfig = builder.Configuration.GetSection("RateScaling");

builder.Services
    .Configure<AppSettings>(appSettingsConfig.Bind)
    .AddHostedService<Worker>()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddSingleton<IFunctionBlockWorker, FunctionBlockWorker>()
    .AddSingleton<IHttpClients, HttpClients>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>()
    .AddFunctionFrameworkFactory<DeviceFunctionFramework, DeviceFunctionFrameworkFactory>()
    .AddRateMonitor()
    .AddResourceMonitor()
    .AddResourceBasedFuzzyRateScaler()
    .AddResourceBasedRateScaling(configure: rateScalingConfig.Bind)
    .AddLimiterManager(configure: (provider, manager) => ConfigureLimiterManager(builder.Configuration, provider, manager))
    .AddSyncAsyncTaskRunner()
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    .AddDefaultBlockRunner()
    .AddDefaultFunctionRunner()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddBlockFrameworkFactory<DeviceBlockFrameworkFactory>()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options =>
    {
        var libraryFolderPath = builder.Configuration["FunctionBlock:JavascriptEngine:LibraryFolderPath"];
        if (!string.IsNullOrEmpty(libraryFolderPath))
            options.LibraryFolderPath = libraryFolderPath;
    });

SetupRabbitMq(services: builder.Services, configuration: builder.Configuration);

builder.Services
    .AddHttpClient(ClientNames.DeviceService, httpClient =>
    {
        httpClient.BaseAddress = new Uri(builder.Configuration["AppSettings:DeviceServiceUrl"]);
    });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.MapPost("/api/fb/workers/scaling/start", (
    [FromServices] IConfiguration configuration,
    [FromServices] ILogger<Program> logger,
    [FromServices] ILimiterManager limiterManager,
    [FromServices] IRateScalingController controller) =>
{
    controller.StartRateCollector(rateLimiters: limiterManager.AllLimiters);
    controller.Start(rateLimiters: limiterManager.AllLimiters);
    return Results.NoContent();
})
.WithDisplayName("Start FB dynamic scaling worker")
.WithName("Start FB dynamic scaling worker");


app.MapPost("/api/fb/workers/scaling/stop", (
    [FromServices] IRateScalingController controller) =>
{
    controller.Stop();
    controller.StopRateCollector();
    return Results.NoContent();
})
.WithDisplayName("Stop FB dynamic scaling worker")
.WithName("Stop FB dynamic scaling worker");


app.MapPut("/api/configs/app-settings", (
    [FromQuery] int? workerCount,
    [FromQuery] int? concurrencyLimit,
    [FromServices] IOptions<AppSettings> appSettings,
    [FromServices] ILimiterManager limiterManager) =>
{
    var appsettingsChanges = new List<string>();
    if (workerCount.HasValue)
    {
        appSettings.Value.WorkerCount = workerCount.Value;
        appsettingsChanges.Add(nameof(appSettings.Value.WorkerCount));
    }
    appSettings.Value.InvokeChanged(appsettingsChanges);

    if (concurrencyLimit.HasValue && limiterManager.TryGetTaskLimiter(ConcurrencyConstants.LimiterNames.TaskLimiter, out var taskLimiter))
        taskLimiter.SetLimit(limit: concurrencyLimit.Value);

    return Results.NoContent();
})
.WithDisplayName("Update app settings")
.WithName("Update app settings");


Setup(app);

app.Run();


static void Setup(WebApplication app)
{
    var provider = app.Services;
    var resourceMonitor = provider.GetService<IResourceMonitor>();
    var configuration = provider.GetService<IConfiguration>();
    var logger = provider.GetService<ILogger<Program>>();

    resourceMonitor.Collected += (o, e) =>
    {
        var (cpu, mem) = e;
        logger.LogInformation("===== Resource consumption =====\nCPU: {Cpu} - Memory: {Memory}", cpu, mem);
    };
    var resourceMonitorInterval = configuration.GetValue<int>("AppSettings:ResourceMonitorInterval");
    resourceMonitor.Start(interval: resourceMonitorInterval);
}

static void SetupRabbitMq(IServiceCollection services, IConfiguration configuration)
{
    var rabbitMqClientOptions = configuration.GetSection("RabbitMqClient");
    var factory = rabbitMqClientOptions.Get<ConnectionFactory>();

    services.AddRabbitMqConnectionManager(
        connectionFactory: factory,
        configureConnectionFactory: SetupRabbitMqConnection
    );
}

static Action<IConnection> SetupRabbitMqConnection(IServiceProvider provider)
{
    var logger = provider.GetRequiredService<ILogger<Program>>();
    void ConfigureConnection(IConnection connection)
    {
        connection.ConnectionShutdown += (sender, e) => OnConnectionShutdown(sender, e, logger);
    }
    return ConfigureConnection;
}

static void OnConnectionShutdown(object sender, ShutdownEventArgs e, ILogger logger)
{
    if (e.Exception != null)
        logger.LogError(e.Exception, "RabbitMQ connection shutdown reason: {Reason} | Message: {Message}", e.Cause, e.Exception?.Message);
    else
        logger.LogInformation("RabbitMQ connection shutdown reason: {Reason}", e.Cause);
}

static void ConfigureLimiterManager(IConfiguration configuration, IServiceProvider provider, ILimiterManager manager)
{
    var taskLimiterConfig = configuration.GetSection("TaskLimiter");
    var taskLimiterOptions = taskLimiterConfig.Get<TaskLimiterOptions>();
    var taskLimiterLogger = provider.GetRequiredService<ILogger<SyncAsyncTaskLimiter>>();
    var taskLimiter = new SyncAsyncTaskLimiter(taskLimiterOptions, logger: taskLimiterLogger);
    manager.AddLimiter(ConcurrencyConstants.LimiterNames.TaskLimiter, taskLimiter);
}