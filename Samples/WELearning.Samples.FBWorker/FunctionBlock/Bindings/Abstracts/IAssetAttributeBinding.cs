using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.FunctionBlock.Bindings.Abstracts;

public interface IAssetAttributeBinding
{
    Task<MetricSeries> LastSeriesBefore(DateTime beforeTime);
    AttributeSnapshot Snapshot { get; }
}
