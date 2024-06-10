using WELearning.ConsoleApp.Testing.Entities;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.ValueObjects;

public class MetricValueObject : RawValueObject
{
    protected readonly MetricSnapshot _snapshot;

    public MetricValueObject(Variable variable, MetricSnapshot snapshot) : base(variable)
    {
        _snapshot = snapshot;
        Value = _snapshot.Value;
    }

    public string Metric => _snapshot.Metric;
    public MetricSnapshot Snapshot => _snapshot;
    public override object Value { get => _snapshot.Value; }

    protected override void SetCoreValue(object value)
    {
        if (value is not double dValue)
            throw new ArgumentException($"Value {value} is not a double");
        _snapshot.Value = dValue;
    }

    public override IValueObject CloneFor(Variable variable) => new MetricValueObject(variable, _snapshot);
}