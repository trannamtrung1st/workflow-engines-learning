
using WELearning.Samples.FBWorker;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Samples.FBWorker.FunctionBlock;
using Confluent.Kafka;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Extensions;
using WELearning.Shared.Extensions;
using WELearning.Core.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

const int minThreads = 512;
int maxThreads = minThreads * 2;
var threadSet = ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
threadSet = ThreadPool.SetMaxThreads(workerThreads: maxThreads, completionPortThreads: maxThreads);

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var taskLimiterConfig = builder.Configuration.GetSection("TaskLimiter");
var consumerConfig = builder.Configuration.GetSection("ConsumerConfig");

builder.Services
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
    .Configure<ConsumerConfig>(config: consumerConfig)
    .AddHostedService<Worker>()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddSingleton<IFunctionBlockWorker, FunctionBlockWorker>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>()
    .AddKafkaClientManager()
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


app.Run();