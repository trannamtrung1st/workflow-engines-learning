using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.Services.Abstracts;

public interface IAssetService
{
    Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes, CancellationToken cancellationToken);
    Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime, CancellationToken cancellationToken);
    Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<string[]> assetAttributes, CancellationToken cancellationToken);
}
