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

const int minThreads = 512;
ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var concurrencyLimit = appSettingsConfig.GetValue<int>("ConcurrencyLimit");

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
    // Function block services
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    .AddDefaultSyncAsyncTaskRunner(initialLimit: concurrencyLimit)
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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


app.MapPut("/api/configs/app-settings", (
    [FromQuery] int? workerCount,
    [FromQuery] int? concurrencyLimit,
    [FromQuery] int? devicesPerInterval,
    [FromQuery] int? simulatorInterval,
    [FromQuery] int? latencyMs,
    [FromServices] IOptions<AppSettings> appSettings) =>
{
    if (workerCount.HasValue) appSettings.Value.WorkerCount = workerCount.Value;
    if (concurrencyLimit.HasValue) appSettings.Value.ConcurrencyLimit = concurrencyLimit.Value;
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
    var fbWorker = app.Services.GetRequiredService<IFunctionBlockWorker>();
    fbWorker.StartWorker(cancellationToken: app.Lifetime.ApplicationStopping);

    var monitoring = app.Services.GetRequiredService<IMonitoring>();
    monitoring.StartReport();

    var appSettingsOpt = app.Services.GetRequiredService<IOptions<AppSettings>>();
    var taskLimiter = app.Services.GetRequiredService<ISyncAsyncTaskLimiter>();
    var appSettings = appSettingsOpt.Value;
    appSettings.Changed += (o, e) => taskLimiter.SetLimit(limit: appSettings.ConcurrencyLimit);
}