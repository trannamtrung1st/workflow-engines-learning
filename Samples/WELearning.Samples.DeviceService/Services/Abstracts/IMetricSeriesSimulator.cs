using WELearning.Samples.DeviceService.Models;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IMetricSeriesSimulator
{
    void StartSimulation();
    void StopSimulation();
    IEnumerable<MetricSeries> GetRandomMetricSeries(string assetId);
}
