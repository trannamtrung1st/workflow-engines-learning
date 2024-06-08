using WELearning.ConsoleApp.Testing.Entities;

namespace WELearning.ConsoleApp.Testing.Framework.Abstracts;

public interface IMetricBinding
{
    Task<MetricSeries> LastSeriesBefore(DateTime beforeTime);
    MetricSnapshot Snapshot { get; }
}
