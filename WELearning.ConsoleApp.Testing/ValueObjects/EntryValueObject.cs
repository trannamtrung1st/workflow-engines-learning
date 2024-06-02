using WELearning.ConsoleApp.Testing.Entities;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.ValueObjects;

public class EntryValueObject : RawValueObject
{
    private readonly EntryEntity _entity;

    public EntryValueObject(Variable variable, EntryEntity entity) : base(variable)
    {
        _entity = entity;
        EntryKey = _entity.Key;
        Value = _entity.Value;
    }

    public EntryValueObject(Variable variable, EntryValueObject clonedFrom) : this(variable, clonedFrom._entity)
    {
    }

    public string EntryKey { get; }
    public override bool IsRaw => false;
    public override object Value { get => _entity.Value; set => base.Value = value; }

    protected override void SetCoreValue(object value) => _entity.Value = value;
    public override IValueObject CloneFor(Variable variable) => new EntryValueObject(variable, this);
}