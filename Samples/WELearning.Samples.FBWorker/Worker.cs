using Microsoft.Extensions.Options;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Shared.Concurrency.Configurations;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Samples.FBWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IOptions<AppSettings> _appSettingsOptions;
    private readonly IOptions<TaskLimiterOptions> _taskLimiterOptions;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IConfiguration _configuration;
    private readonly IFunctionBlockWorker _fbWorker;
    private readonly IRateMonitor _rateMonitor;

    public Worker(
        ILogger<Worker> logger,
        IOptions<AppSettings> appSettingsOptions,
        IOptions<TaskLimiterOptions> taskLimiterOptions,
        IResourceMonitor resourceMonitor,
        IConfiguration configuration,
        IFunctionBlockWorker fbWorker,
        IRateMonitor rateMonitor)
    {
        _logger = logger;
        _appSettingsOptions = appSettingsOptions;
        _taskLimiterOptions = taskLimiterOptions;
        _resourceMonitor = resourceMonitor;
        _configuration = configuration;
        _fbWorker = fbWorker;
        _rateMonitor = rateMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Setup();

        _fbWorker.StartWorker(cancellationToken: stoppingToken);
        _rateMonitor.StartReport();

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    private void Setup()
    {
        var appSettings = _appSettingsOptions.Value;

        var idealUsage = _configuration.GetValue<double>("AppSettings:IdealUsage");
        appSettings.WorkerCount = (int)(_resourceMonitor.TotalCores * idealUsage); // [NOTE] default worker count
        _taskLimiterOptions.Value.AvailableCores = _resourceMonitor.TotalCores;

        if (appSettings.WorkerCount < 1)
            appSettings.WorkerCount = 1;

        _logger.LogInformation("Default worker count: {WorkerCount}", appSettings.WorkerCount);
    }
}
