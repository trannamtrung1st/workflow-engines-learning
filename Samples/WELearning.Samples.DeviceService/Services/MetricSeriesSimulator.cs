using Microsoft.Extensions.Options;
using WELearning.Samples.DeviceService.Configurations;
using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class MetricSeriesSimulator : IMetricSeriesSimulator, IDisposable
{
    private readonly DataStore _dataStore;
    private readonly ILogger<MetricSeriesSimulator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly System.Timers.Timer _timer;
    private string _demoBlockId;

    public MetricSeriesSimulator(
        DataStore dataStore,
        IOptions<AppSettings> appSettings,
        ILogger<MetricSeriesSimulator> logger,
        IServiceProvider serviceProvider)
    {
        _dataStore = dataStore;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appSettings = appSettings;

        _timer = new System.Timers.Timer(interval: _appSettings.Value.SimulatorInterval);
        _timer.AutoReset = true;
        _timer.Elapsed += async (o, e) => await SimulateNewMetricSeries();
        _appSettings.Value.Changed += HandleAppSettingsChanged;
    }

    private void HandleAppSettingsChanged(object sender, IEnumerable<string> changes)
    {
        if (_timer.Interval != _appSettings.Value.SimulatorInterval)
            _timer.Interval = _appSettings.Value.SimulatorInterval;
    }

    public void StartSimulation(string demoBlockId)
    {
        _demoBlockId = demoBlockId;
        _timer.Start();
    }

    public void StopSimulation() => _timer.Stop();

    public IEnumerable<MetricSeries> GetRandomMetricSeries(string assetId)
    {
        var dym1 = new MetricSeries(
            assetId: assetId,
            attributeName: "dynamic1",
            value: Random.Shared.Next(maxValue: 100));

        var dym2 = new MetricSeries(
            assetId: assetId,
            attributeName: "dynamic2",
            value: Random.Shared.Next(maxValue: 100));

        return new[] { dym1, dym2 };
    }

    private async Task SimulateNewMetricSeries()
    {
        try
        {
            var assetIdx = Random.Shared.Next(maxValue: _dataStore.NoOfAssets);
            int count = 0;
            using var scope = _serviceProvider.CreateScope();
            var assetService = scope.ServiceProvider.GetRequiredService<IAssetService>();

            while (count <= _appSettings.Value.DevicesPerInterval)
            {
                await assetService.AddMetricSeries(
                    series: GetRandomMetricSeries($"asset-{assetIdx}"),
                    demoBlockId: _demoBlockId);

                count++;
                assetIdx++;
                assetIdx = assetIdx == _dataStore.NoOfAssets ? 0 : assetIdx;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _appSettings.Value.Changed -= HandleAppSettingsChanged;
        _timer?.Dispose();
    }
}