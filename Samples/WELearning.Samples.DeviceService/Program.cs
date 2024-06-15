using Microsoft.AspNetCore.Mvc;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Shared.Concurrency.Extensions;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Core.Reflection.Extensions;
using WELearning.ConsoleApp.Testing.Framework;
using WELearning.Samples.DeviceService.Configurations;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Diagnostic.Extensions;
using WELearning.Shared.Concurrency.Configurations;
using WELearning.Shared.Diagnostic.Abstracts;

const int minThreads = 512;
int maxThreads = minThreads * 2;
var threadSet = ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
threadSet = ThreadPool.SetMaxThreads(workerThreads: maxThreads, completionPortThreads: maxThreads);

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var taskLimiterConfig = builder.Configuration.GetSection("TaskLimiter");
var initialConcurrencyLimit = appSettingsConfig.GetValue<int>("InitialConcurrencyLimit");

builder.Services
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddSingleton<DataStore>()
    .AddSingleton<IFunctionBlockWorker, FunctionBlockWorker>()
    .AddSingleton<IMessageQueue, MessageQueue>()
    .AddSingleton<IMetricSeriesSimulator, MetricSeriesSimulator>()
    .AddSingleton<IMonitoring, Monitoring>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>()
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/api/assets/{id}/snapshot", async (string id, [FromServices] IAssetService assetService) =>
{
    var assetSnapshot = await assetService.GetAssetSnapshot(assetId: id);
    return assetSnapshot;
})
.WithDisplayName("Get asset snapshot")
.WithName("Get asset snapshot");


app.MapPost("/api/series/simulation/{demoBlockId}", async (
    [FromQuery] string assetId,
    [FromRoute] string demoBlockId,
    [FromServices] IMetricSeriesSimulator simulator,
    [FromServices] IAssetService assetService) =>
{
    var series = simulator.GetRandomMetricSeries(assetId);
    await assetService.AddMetricSeries(series, demoBlockId);
    return Results.NoContent();
})
.WithDisplayName("Add random series simulation")
.WithName("Add random series simulation");


app.MapPost("/api/series/simulation/{demoBlockId}/start", (
    [FromRoute] string demoBlockId,
    [FromServices] IMetricSeriesSimulator simulator) =>
{
    simulator.StartSimulation(demoBlockId);
    return Results.NoContent();
})
.WithDisplayName("Start series simulation")
.WithName("Start series simulation");


app.MapPost("/api/series/simulation/stop", ([FromServices] IMetricSeriesSimulator simulator) =>
{
    simulator.StopSimulation();
    return Results.NoContent();
})
.WithDisplayName("Stop series simulation")
.WithName("Stop series simulation");


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
    [FromQuery] int? devicesPerInterval,
    [FromQuery] int? simulatorInterval,
    [FromQuery] int? latencyMs,
    [FromServices] IOptions<AppSettings> appSettings) =>
{
    if (workerCount.HasValue) appSettings.Value.WorkerCount = workerCount.Value;
    if (initialConcurrencyLimit.HasValue) appSettings.Value.InitialConcurrencyLimit = initialConcurrencyLimit.Value;
    if (devicesPerInterval.HasValue) appSettings.Value.DevicesPerInterval = devicesPerInterval.Value;
    if (simulatorInterval.HasValue) appSettings.Value.SimulatorInterval = simulatorInterval.Value;
    if (latencyMs.HasValue) appSettings.Value.LatencyMs = latencyMs.Value;
    appSettings.Value.InvokeChanged();
    return Results.NoContent();
})
.WithDisplayName("Update app settings")
.WithName("Update app settings");


Setup(app);

app.Run();

static void Setup(WebApplication app)
{
    var provider = app.Services;

    var appSettingsOpt = provider.GetRequiredService<IOptions<AppSettings>>();
    var taskLimiter = provider.GetRequiredService<ISyncAsyncTaskLimiter>();
    var appSettings = appSettingsOpt.Value;
    appSettings.Changed += (o, e) => taskLimiter.SetLimit(limit: appSettings.InitialConcurrencyLimit);

    var taskLimiterOpt = provider.GetRequiredService<IOptions<TaskLimiterOptions>>();
    var resourceMonitor = provider.GetRequiredService<IResourceMonitor>();
    var programLogger = provider.GetRequiredService<ILogger<Program>>();
    var idealUsage = app.Configuration.GetValue<double>("AppSettings:IdealUsage");

    appSettings.WorkerCount = (int)(resourceMonitor.TotalCores * idealUsage); // [NOTE] default worker count
    taskLimiterOpt.Value.AvailableCores = resourceMonitor.TotalCores;
    if (appSettings.WorkerCount < 1) appSettings.WorkerCount = 1;
    programLogger.LogInformation("Default worker count: {WorkerCount}", appSettings.WorkerCount);

    var fbWorker = provider.GetRequiredService<IFunctionBlockWorker>();
    fbWorker.StartWorker(cancellationToken: app.Lifetime.ApplicationStopping);

    var monitoring = provider.GetRequiredService<IMonitoring>();
    monitoring.StartReport();
}