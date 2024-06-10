using WELearning.Samples.DeviceService.Models;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IAssetService
{
    Task<AssetSnapshot> GetAssetSnapshot(string assetId);
    Task AddMetricSeries(IEnumerable<MetricSeries> series);
    Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes);
    Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime);
    Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<(string AssetId, string AttributeName)> assetAttributes);
}
