using WELearning.ConsoleApp.Testing.Entities;
using WELearning.ConsoleApp.Testing.Framework.Abstracts;
using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework.Bindings;

public class MetricBinding : ReadWriteBinding, IMetricBinding
{
    private readonly DataStore _dataStore;
    private new readonly MetricValueObject _valueObject;

    public MetricBinding(string name, MetricValueObject valueObject, DataStore dataStore) : base(name, valueObject)
    {
        _valueObject = valueObject;
        _dataStore = dataStore;
    }

    public string Metric => _valueObject.Metric;
    public MetricSnapshot Snapshot => _valueObject.Snapshot;

    public Task<MetricSeries> LastSeriesBefore(DateTime beforeTime)
    {
        var series = _dataStore.LastSeriesBefore(metric: Metric, beforeTime);
        return Task.FromResult(series);
    }
}

public class ReadMetricBinding : IReadBinding, IMetricBinding
{
    private readonly MetricBinding _metricBinding;
    public ReadMetricBinding(string name, MetricValueObject valueObject, DataStore dataStore)
    {
        _metricBinding = new MetricBinding(name, valueObject, dataStore);
    }

    public object Value => _metricBinding.Value;
    public bool IsNumeric => _metricBinding.IsNumeric;
    public string Name => _metricBinding.Name;
    public bool ValueSet => _metricBinding.ValueSet;
    public string Metric => _metricBinding.Metric;
    public MetricSnapshot Snapshot => _metricBinding.Snapshot;

    public double AsDouble() => _metricBinding.AsDouble();
    public int AsInt() => _metricBinding.AsInt();
    public Task<MetricSeries> LastSeriesBefore(DateTime beforeTime) => _metricBinding.LastSeriesBefore(beforeTime);
}