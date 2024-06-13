using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Samples.DeviceService.Models;

namespace WELearning.Samples.DeviceService.FunctionBlock.ValueObjects;

public class AttributeValueObject : RawValueObject
{
    protected readonly AttributeSnapshot _snapshot;

    public AttributeValueObject(Variable variable, AttributeSnapshot snapshot) : base(variable)
    {
        _snapshot = snapshot;
        Value = _snapshot.Value;
    }

    public string AttributeName => _snapshot.AttributeName;
    public AttributeSnapshot Snapshot => _snapshot;
    public override object Value { get => _snapshot.Value; }

    protected override void SetCoreValue(object value)
    {
        if (value == null)
        {
            _snapshot.Value = null;
            return;
        }

        if (value is not double dValue)
            throw new ArgumentException($"Value {value} is not a double");
        _snapshot.Value = dValue;
    }

    public override IValueObject CloneFor(Variable variable) => new AttributeValueObject(variable, _snapshot);

    public override object GetProperty(string name)
    {
        switch (name)
        {
            case nameof(Snapshot.Timestamp): return Snapshot?.Timestamp;
            default: return base.GetProperty(name);
        }
    }
}