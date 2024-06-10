using WELearning.Samples.DeviceService.Models;

namespace WELearning.Samples.DeviceService.FunctionBlock.Bindings.Abstracts;

public interface IAssetAttributeBinding
{
    Task<MetricSeries> LastSeriesBefore(DateTime beforeTime);
    AttributeSnapshot Snapshot { get; }
}
