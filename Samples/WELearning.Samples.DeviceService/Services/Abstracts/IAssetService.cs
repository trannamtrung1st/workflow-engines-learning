using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IAssetService
{
    Task<AssetSnapshot> GetAssetSnapshot(string assetId);
    Task AddMetricSeries(IEnumerable<MetricSeries> series, string demoBlockId);
    Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes);
    Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime);
    Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<string[]> assetAttributes);
}
