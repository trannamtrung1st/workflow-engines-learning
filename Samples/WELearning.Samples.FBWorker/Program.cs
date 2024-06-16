
using WELearning.Samples.FBWorker;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Samples.FBWorker.FunctionBlock;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Extensions;
using WELearning.Shared.Extensions;
using WELearning.Core.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

const int minThreads = 512;
int maxThreads = minThreads * 2;
var threadSet = ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
threadSet = ThreadPool.SetMaxThreads(workerThreads: maxThreads, completionPortThreads: maxThreads);

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var taskLimiterConfig = builder.Configuration.GetSection("TaskLimiter");

builder.Services
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
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
    .AddRateMonitor()
    .AddResourceMonitor()
    .AddFuzzyThreadController()
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    .AddDefaultSyncAsyncTaskRunner(configure: (options) => taskLimiterConfig.Bind(options))
    .AddDefaultBlockRunner()
    .AddDefaultFunctionRunner()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddBlockFrameworkFactory<DeviceBlockFrameworkFactory>()
    .AddFunctionFramework<DeviceFunctionFramework>()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options =>
    {
        var libraryFolderPath = builder.Configuration["FunctionBlock:JavascriptEngine:LibraryFolderPath"];
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


app.MapPost("/api/fb/workers/scaling/start", ([FromServices] IFunctionBlockWorker fbWorker) =>
{
    fbWorker.StartDynamicScalingWorker();
    return Results.NoContent();
})
.WithDisplayName("Start FB dynamic scaling worker")
.WithName("Start FB dynamic scaling worker");


app.MapPost("/api/fb/workers/scaling/stop", ([FromServices] IFunctionBlockWorker fbWorker) =>
{
    fbWorker.StopDynamicScalingWorker();
    return Results.NoContent();
})
.WithDisplayName("Stop FB dynamic scaling worker")
.WithName("Stop FB dynamic scaling worker");


app.MapPut("/api/configs/app-settings", (
    [FromQuery] int? workerCount,
    [FromQuery] int? initialConcurrencyLimit,
    [FromServices] IOptions<AppSettings> appSettings) =>
{
    var changes = new List<string>();
    if (workerCount.HasValue)
    {
        appSettings.Value.WorkerCount = workerCount.Value;
        changes.Add(nameof(appSettings.Value.WorkerCount));
    }
    if (initialConcurrencyLimit.HasValue)
    {
        appSettings.Value.InitialConcurrencyLimit = initialConcurrencyLimit.Value;
        changes.Add(nameof(appSettings.Value.InitialConcurrencyLimit));
    }
    appSettings.Value.InvokeChanged(changes);
    return Results.NoContent();
})
.WithDisplayName("Update app settings")
.WithName("Update app settings");


Setup(app);

app.Run();


static void Setup(WebApplication app)
{
    var provider = app.Services;
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
