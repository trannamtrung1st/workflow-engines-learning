using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class MetricSeriesSimulator : IMetricSeriesSimulator
{
    private readonly DataStore _dataStore;
    private readonly System.Timers.Timer _timer;
    private readonly ILogger<MetricSeriesSimulator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _devicesPerInterval;

    public MetricSeriesSimulator(
        DataStore dataStore,
        IConfiguration configuration,
        ILogger<MetricSeriesSimulator> logger,
        IServiceProvider serviceProvider)
    {
        _dataStore = dataStore;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _devicesPerInterval = configuration.GetValue<int>("AppSettings:DevicesPerInterval");

        var simulatorInterval = configuration.GetValue<int>("AppSettings:SimulatorInterval");
        _timer = new System.Timers.Timer(simulatorInterval);
        _timer.AutoReset = true;
        _timer.Elapsed += async (o, e) => await SimulateNewMetricSeries();
    }

    public void StartSimulation() => _timer.Start();

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

            while (count <= _devicesPerInterval)
            {

                await assetService.AddMetricSeries(GetRandomMetricSeries($"asset-{assetIdx}"));

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
}