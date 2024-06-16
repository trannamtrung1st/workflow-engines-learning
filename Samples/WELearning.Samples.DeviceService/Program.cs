using Microsoft.AspNetCore.Mvc;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.DeviceService.Configurations;
using Microsoft.Extensions.Options;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;
using WELearning.Shared.Diagnostic.Abstracts;
using WELearning.Shared.Diagnostic;

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
    .AddSingleton<IMetricSeriesSimulator, MetricSeriesSimulator>()
    .AddSingleton<IRateMonitor, RateMonitor>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>();

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


app.MapPut("/api/configs/app-settings", (
    [FromQuery] int? workerCount,
    [FromQuery] int? initialConcurrencyLimit,
    [FromQuery] int? devicesPerInterval,
    [FromQuery] int? simulatorInterval,
    [FromQuery] int? latencyMs,
    [FromServices] IOptions<AppSettings> appSettings) =>
{
    var changes = new List<string>();
    if (devicesPerInterval.HasValue)
    {
        appSettings.Value.DevicesPerInterval = devicesPerInterval.Value;
        changes.Add(nameof(appSettings.Value.DevicesPerInterval));
    }
    if (simulatorInterval.HasValue)
    {
        appSettings.Value.SimulatorInterval = simulatorInterval.Value;
        changes.Add(nameof(appSettings.Value.SimulatorInterval));
    }
    if (latencyMs.HasValue)
    {
        appSettings.Value.LatencyMs = latencyMs.Value;
        changes.Add(nameof(appSettings.Value.LatencyMs));
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

    var appSettingsOpt = provider.GetRequiredService<IOptions<AppSettings>>();
    var taskLimiter = provider.GetRequiredService<ISyncAsyncTaskLimiter>();
    var appSettings = appSettingsOpt.Value;

    var taskLimiterOpt = provider.GetRequiredService<IOptions<TaskLimiterOptions>>();
    var resourceMonitor = provider.GetRequiredService<IResourceMonitor>();
    var programLogger = provider.GetRequiredService<ILogger<Program>>();
    var idealUsage = app.Configuration.GetValue<double>("AppSettings:IdealUsage");

    var rateMonitor = provider.GetRequiredService<IRateMonitor>();
    rateMonitor.StartReport();
}