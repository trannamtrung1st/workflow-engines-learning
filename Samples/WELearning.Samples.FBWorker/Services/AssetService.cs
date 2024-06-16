using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.Services;

// [TODO]
public class AssetService : IAssetService
{
    public Task AddMetricSeries(IEnumerable<MetricSeries> series, string demoBlockId)
    {
        throw new NotImplementedException();
    }

    public Task<AssetSnapshot> GetAssetSnapshot(string assetId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<(string AssetId, string AttributeName)> assetAttributes)
    {
        throw new NotImplementedException();
    }

    public Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime)
    {
        throw new NotImplementedException();
    }

    public Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes)
    {
        throw new NotImplementedException();
    }
}
