using Microsoft.AspNetCore.Mvc;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.DeviceService.Configurations;
using Microsoft.Extensions.Options;
using WELearning.Shared.Diagnostic.Abstracts;
using WELearning.Shared.Extensions;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using WELearning.Samples.Shared.Extensions;
using WELearning.Samples.Shared.Kafka.Abstracts;
using WELearning.Samples.Shared.Models;
using WELearning.Samples.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var producerConfig = builder.Configuration.GetSection("ProducerConfig");
var adminClientConfig = builder.Configuration.GetSection("AdminClientConfig");

builder.Services
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
    .Configure<ProducerConfig>(config: producerConfig)
    .Configure<AdminClientConfig>(config: adminClientConfig)
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddRateMonitor()
    .AddKafkaClientManager()
    .AddSingleton<DataStore>()
    .AddSingleton<IMetricSeriesSimulator, MetricSeriesSimulator>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>();

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
.WithName("Get asset attributes snapshots");


app.MapPut("/api/assets/attributes/snapshots", async (
    [FromBody] IEnumerable<AttributeSnapshot> attributes,
    [FromServices] IAssetService assetService) =>
{
    await assetService.UpdateRuntime(attributes);
    return Results.NoContent();
})
.WithDisplayName("Update asset attributes snapshots")
.WithName("Update asset attributes snapshots");


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
.WithName("Get asset attribute series");


app.MapGet("/api/fb/{id}", async (string id, [FromServices] IFunctionBlockService fbService) =>
{
    var cfb = await fbService.GetBlockDefinitions(id);
    return cfb;
})
.WithDisplayName("Get composite block definition")
.WithName("Get composite block definition");


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


await Setup(app);

app.Run();

static async Task Setup(WebApplication app)
{
    var provider = app.Services;
    var rateMonitor = provider.GetRequiredService<IRateMonitor>();
    rateMonitor.StartReport();

    var kafkaClientManager = provider.GetRequiredService<IKafkaClientManager>();
    var adminClientOptions = provider.GetRequiredService<IOptions<AdminClientConfig>>();
    using var adminClient = kafkaClientManager.GetAdminClient(adminClientOptions.Value);
    var existingTopics = adminClient
        .GetMetadata(timeout: TimeSpan.FromSeconds(5)).Topics
        .Select(t => t.Topic).ToHashSet();
    var topicSpecs = new List<TopicSpecification>();
    topicSpecs.Add(new() { Name = TopicNames.AttributeChanged, ReplicationFactor = 1, NumPartitions = 20 });
    topicSpecs = topicSpecs.Where(t => !existingTopics.Contains(t.Name)).ToList();

    if (topicSpecs.Count > 0)
        await adminClient.CreateTopicsAsync(topicSpecs);
}