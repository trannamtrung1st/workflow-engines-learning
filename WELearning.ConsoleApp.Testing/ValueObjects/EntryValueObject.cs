using WELearning.ConsoleApp.Testing.Entities;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.ValueObjects;

public abstract class EntryValueObject : RawValueObject
{
    protected readonly EntryEntity _entity;

    public EntryValueObject(Variable variable, EntryEntity entity) : base(variable)
    {
        _entity = entity;
        EntryKey = _entity.Key;
    }

    public string EntryKey { get; }
    public override object Value { get => _entity.Value; set => base.Value = value; }

    protected override void SetCoreValue(object value) => _entity.Value = value;
}

public class RWEntryValueObject : EntryValueObject
{
    public RWEntryValueObject(Variable variable, EntryEntity entity) : base(variable, entity)
    {
        Value = _entity.Value;
        // [NOTE] value is set
    }

    public override IValueObject CloneFor(Variable variable) => new RWEntryValueObject(variable, _entity);
}

public class REntryValueObject : EntryValueObject
{
    public REntryValueObject(Variable variable, EntryEntity entity) : base(variable, entity)
    {
        _valueSet.Set();
        ValueChanged = false;
        // [NOTE] value is set
    }

    public override object Value
    {
        set => throw new InvalidOperationException("Read-only!");
    }

    public override IValueObject CloneFor(Variable variable) => new REntryValueObject(variable, _entity);
}

public class WEntryValueObject : EntryValueObject
{
    public WEntryValueObject(Variable variable, EntryEntity entity) : base(variable, entity)
    {
        // [NOTE] value is not set
    }

    private object _value;
    public override object Value
    {
        get => _value;
    }

    protected override void SetCoreValue(object value)
    {
        _value = value;
        base.SetCoreValue(value);
    }

    public override IValueObject CloneFor(Variable variable) => new WEntryValueObject(variable, _entity);
}
