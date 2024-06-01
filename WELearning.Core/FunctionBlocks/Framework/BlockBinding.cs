using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

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

public class InputBinding : BlockBinding, IReadBinding
{
    public InputBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
    }

    public virtual object Value => _valueObject.Value;
    public virtual bool IsNumeric => _valueObject.IsNumeric;
    public double AsDouble() => _valueObject.AsDouble();
    public int AsInt() => _valueObject.AsInt();
}

public class OutputBinding : BlockBinding, IWriteBinding
{
    public OutputBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
    }

    public virtual Task Write(object value)
    {
        _valueObject.TempValue = value;
        return Task.CompletedTask;
    }
}

public class InOutBinding : BlockBinding, IReadWriteBinding
{
    private readonly InputBinding _inputBinding;
    private readonly OutputBinding _outputBinding;

    public InOutBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
        _inputBinding = new(name, valueObject);
        _outputBinding = new(name, valueObject);
    }

    public object Value => _inputBinding.Value;
    public bool IsNumeric => _inputBinding.IsNumeric;
    public double AsDouble() => _inputBinding.AsDouble();
    public int AsInt() => _inputBinding.AsInt();
    public Task Write(object value) => _outputBinding.Write(value);
}

public class InternalBinding : BlockBinding, IReadWriteBinding
{
    private readonly InputBinding _inputBinding;

    public InternalBinding(string name, IValueObject valueObject) : base(name, valueObject)
    {
        _inputBinding = new(name, valueObject);
    }

    public object Value => _inputBinding.Value;
    public bool IsNumeric => _inputBinding.IsNumeric;
    public double AsDouble() => _inputBinding.AsDouble();
    public int AsInt() => _inputBinding.AsInt();
    public Task Write(object value)
    {
        _valueObject.Value = value;
        return Task.CompletedTask;
    }
}
