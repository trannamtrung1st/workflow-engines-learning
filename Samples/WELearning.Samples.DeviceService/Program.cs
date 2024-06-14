using Microsoft.AspNetCore.Mvc;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Shared.Concurrency.Extensions;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Core.Reflection.Extensions;
using WELearning.ConsoleApp.Testing.Framework;

const int minThreads = 512;
ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);

var builder = WebApplication.CreateBuilder(args);
var initialConcurrencyLimit = builder.Configuration.GetValue<int>("AppSettings:InitialConcurrencyLimit");

builder.Services
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
    .AddDefaultSyncAsyncTaskRunner(initialLimit: initialConcurrencyLimit)
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
    return Results.Accepted();
})
.WithDisplayName("Start series simulation")
.WithName("Start series simulation");


app.MapPost("/api/series/simulation/stop", ([FromServices] IMetricSeriesSimulator simulator) =>
{
    simulator.StopSimulation();
    return Results.Accepted();
})
.WithDisplayName("Stop series simulation")
.WithName("Stop series simulation");

StartWorkers(app);

app.Run();

static void StartWorkers(WebApplication app)
{
    var fbWorker = app.Services.GetRequiredService<IFunctionBlockWorker>();
    fbWorker.StartWorker(cancellationToken: app.Lifetime.ApplicationStopping);

    var monitoring = app.Services.GetRequiredService<IMonitoring>();
    monitoring.StartReport();
}