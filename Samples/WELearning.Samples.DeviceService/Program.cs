using Microsoft.AspNetCore.Mvc;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.DeviceService.Configurations;
using Microsoft.Extensions.Options;
using WELearning.Shared.Diagnostic.Abstracts;
using WELearning.Shared.Extensions;
using WELearning.Samples.Shared.Models;
using WELearning.Samples.Shared.Constants;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");

builder.Services
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddRateMonitor()
    .AddSingleton<DataStore>()
    .AddSingleton<IMetricSeriesSimulator, MetricSeriesSimulator>()
    .AddSingleton<IHttpClients, HttpClients>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>();

builder.Services
    .AddHttpClient(ClientNames.DeviceService);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/api/assets/{id}/snapshot", async (string id, [FromServices] IAssetService assetService) =>
{
    var assetSnapshot = await assetService.GetAssetSnapshot(assetId: id);
    return assetSnapshot;
})
.WithDisplayName("Get asset snapshot")
.WithName("Get asset snapshot");


app.MapPost("/api/assets/attributes/snapshots", async (
    [FromBody] IEnumerable<string[]> assetAttributes,
    [FromServices] IAssetService assetService) =>
{
    var snapshots = await assetService.GetSnapshots(assetAttributes);
    return snapshots;
})
.WithDisplayName("Get asset attributes snapshots")
.WithName("Get asset attributes snapshots")
.ExcludeFromDescription();


app.MapPut("/api/assets/attributes/snapshots", async (
    [FromBody] IEnumerable<AttributeSnapshot> attributes,
    [FromServices] IAssetService assetService) =>
{
    await assetService.UpdateRuntime(attributes);
    return Results.NoContent();
})
.WithDisplayName("Update asset attributes snapshots")
.WithName("Update asset attributes snapshots")
.ExcludeFromDescription();


app.MapGet("/api/assets/{assetId}/attributes/{attributeName}/series", async (
    string assetId,
    string attributeName,
    [FromQuery] DateTime beforeTime,
    [FromServices] IAssetService assetService) =>
{
    var series = await assetService.LastSeriesBefore(assetId, attributeName, beforeTime);
    return series;
})
.WithDisplayName("Get asset attribute series")
.WithName("Get asset attribute series")
.ExcludeFromDescription();


app.MapGet("/api/fb/{id}", async (string id, [FromServices] IFunctionBlockService fbService) =>
{
    var cfb = await fbService.GetBlockDefinitions(id);
    return cfb;
})
.WithDisplayName("Get composite block definition")
.WithName("Get composite block definition")
.ExcludeFromDescription();


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


#region FBWorker API

app.MapPost("/api/fb/workers/{host}/scaling/start", async (string host, [FromServices] IHttpClients clients) =>
{
    var builder = new UriBuilder();
    builder.Scheme = "http";
    builder.Host = host;
    builder.Path = "/api/fb/workers/scaling/start";

    var resp = await clients.FBWorker.PostAsJsonAsync(builder.Uri, value: default(object));
    return Results.StatusCode((int)resp.StatusCode);
})
.WithDisplayName("Start FB dynamic scaling worker")
.WithName("Start FB dynamic scaling worker");


app.MapPost("/api/fb/workers/{host}/scaling/stop", async (string host, [FromServices] IHttpClients clients) =>
{
    var builder = new UriBuilder();
    builder.Scheme = "http";
    builder.Host = host;
    builder.Path = "/api/fb/workers/scaling/stop";

    var resp = await clients.FBWorker.PostAsJsonAsync(builder.Uri, value: default(object));
    return Results.StatusCode((int)resp.StatusCode);
})
.WithDisplayName("Stop FB dynamic scaling worker")
.WithName("Stop FB dynamic scaling worker");


app.MapPut("/api/fb/workers/{host}/configs/app-settings", async (
    string host,
    [FromQuery] int? workerCount,
    [FromQuery] int? initialConcurrencyLimit,
    [FromServices] IHttpClients clients) =>
{
    var builder = new UriBuilder();
    builder.Scheme = "http";
    builder.Host = host;
    builder.Path = "/api/configs/app-settings";
    var queryBuilder = new QueryBuilder();
    queryBuilder.Add(nameof(workerCount), workerCount?.ToString());
    queryBuilder.Add(nameof(initialConcurrencyLimit), initialConcurrencyLimit?.ToString());
    builder.Query = queryBuilder.ToString();

    var resp = await clients.FBWorker.PutAsJsonAsync(builder.Uri, value: default(object));
    return Results.StatusCode((int)resp.StatusCode);
})
.WithDisplayName("Update worker app settings")
.WithName("Update worker app settings");

#endregion

Setup(app);

app.Run();

static void Setup(WebApplication app)
{
    var provider = app.Services;
    var rateMonitor = provider.GetRequiredService<IRateMonitor>();
    rateMonitor.StartReport();
}