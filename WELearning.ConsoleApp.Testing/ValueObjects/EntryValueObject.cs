using WELearning.ConsoleApp.Testing.Entities;
using WELearning.Core.Common;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.ValueObjects;

public class EntryValueObject : RawValueObject
{
    private readonly ValueRef<object> _valueRef;

    public EntryValueObject(Variable variable, EntryEntity entity) : base(variable)
    {
        _valueRef = new(entity.Value);
        EntryKey = entity.Key;
        Value = _valueRef.Value;
    }

    public EntryValueObject(Variable variable, EntryValueObject clonedFrom) : base(variable)
    {
        _valueRef = clonedFrom._valueRef;
        EntryKey = clonedFrom.EntryKey;
        Value = _valueRef.Value;
    }

    public string EntryKey { get; }
    public override bool IsRaw => false;
    public override object Value { get => _valueRef.Value; set => base.Value = value; }

    protected override void SetCoreValue(object value) => _valueRef.Value = value;
    public override IValueObject CloneFor(Variable variable) => new EntryValueObject(variable, this);
}