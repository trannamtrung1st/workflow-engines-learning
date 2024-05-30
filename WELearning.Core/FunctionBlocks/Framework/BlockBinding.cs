using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockBinding : IReadWriteBinding
{
    private readonly ValueObject _valueObject;
    public BlockBinding(string name, ValueObject valueObject)
    {
        Name = name;
        _valueObject = valueObject;
    }

    public virtual string Name { get; protected set; }
    public virtual object Value => _valueObject.Value;
    public virtual bool ValueSet => _valueObject.ValueSet;
    public virtual bool IsNumeric => _valueObject.IsNumeric;
    public virtual bool Is<T>() => _valueObject.Is<T>();
    public double ToDouble() => _valueObject.ToDouble();
    public int ToInt() => _valueObject.ToInt();

    public virtual Task Set(object value)
    {
        _valueObject.Value = value;
        return Task.CompletedTask;
    }
}
