using WELearning.ConsoleApp.Testing.Entities;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.ValueObjects;

public class EntryValueObject : RawValueObject
{
    protected readonly EntryEntity _entity;

    public EntryValueObject(Variable variable, EntryEntity entity) : base(variable)
    {
        _entity = entity;
        Value = _entity.Value;
    }

    public string EntryKey => _entity.Key;
    public override object Value { get => _entity.Value; set => base.Value = value; }

    protected override void SetCoreValue(object value) => _entity.Value = value;

    public override IValueObject CloneFor(Variable variable) => new EntryValueObject(variable, _entity);
}