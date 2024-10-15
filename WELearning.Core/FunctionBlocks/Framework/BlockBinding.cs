using WELearning.Core.Common.Extensions;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Helpers;

namespace WELearning.Core.FunctionBlocks.Framework;

// [IMPORTANT] do not expose complex types cause Jint engine requires proxy which won't work for every types
public abstract class BlockBinding : IBlockBinding
{
    protected readonly IValueObject _valueObject;
    public BlockBinding(string name, IValueObject valueObject)
    {
        Name = name;
        _valueObject = valueObject;
    }
    public virtual string Name { get; protected set; }
    public virtual bool ValueSet => _valueObject.ValueSet;
}

public class ReadBinding : BlockBinding, IReadBinding
{
    public ReadBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
    }

    public virtual object Value => _valueObject.Value;
    public virtual bool IsNumeric => _valueObject.IsNumeric;
    public virtual double AsDouble() => _valueObject.Value.AsDouble().Value;
    public virtual int AsInt() => _valueObject.Value.AsInt().Value;
}

public class WriteBinding : BlockBinding, IWriteBinding
{
    public WriteBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
    }

    public virtual void Write(object value) => _valueObject.TempValue = BindingHelper.GetValue(value);
}

public class ReadWriteBinding : BlockBinding, IReadWriteBinding
{
    private readonly ReadBinding _inputBinding;
    private readonly WriteBinding _outputBinding;

    public ReadWriteBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
        _inputBinding = new(name, valueObject);
        _outputBinding = new(name, valueObject);
    }

    public virtual object Value => _inputBinding.Value;
    public virtual bool IsNumeric => _inputBinding.IsNumeric;
    public virtual double AsDouble() => _inputBinding.AsDouble();
    public virtual int AsInt() => _inputBinding.AsInt();
    public virtual void Write(object value) => _outputBinding.Write(value);
}

public class InternalBinding : BlockBinding, IReadWriteBinding
{
    private readonly ReadBinding _readBinding;
    public InternalBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
        _readBinding = new(name, valueObject);
    }

    public virtual object Value => _readBinding.Value;
    public virtual bool IsNumeric => _readBinding.IsNumeric;
    public virtual double AsDouble() => _readBinding.AsDouble();
    public virtual int AsInt() => _readBinding.AsInt();
    public virtual void Write(object value) => _valueObject.TryCastAndSet(value: BindingHelper.GetValue(value));
}
