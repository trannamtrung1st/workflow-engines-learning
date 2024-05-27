using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockBinding : IBlockBinding
{
    private readonly ValueObject _valueObject;
    private readonly IBlockExecutionControl _control;
    public BlockBinding(string name, IBlockExecutionControl control, bool isInternal)
    {
        _control = control;
        Name = name;
        if (isInternal)
        {
            IsInput = false; IsOutput = false; IsInternal = true;
            var valueObject = control.GetInternalData(name);
            _valueObject = valueObject;
        }
        else if ((_valueObject = control.GetInput(name)).ValueSet.IsSet)
        {
            IsInput = true; IsOutput = false; IsInternal = false;
        }
        else
        {
            _valueObject = control.GetOutput(name);
            IsInput = false; IsOutput = true; IsInternal = false;
        }
    }

    public virtual string Name { get; protected set; }
    public virtual object Value => _valueObject.Value;
    public virtual bool ValueSet => _valueObject.ValueSet.IsSet;
    public virtual bool IsInput { get; protected set; }
    public virtual bool IsOutput { get; protected set; }
    public virtual bool IsInternal { get; protected set; }

    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public virtual bool IsNumeric => Value != null && (
        Value is sbyte || Value is byte || Value is short || Value is ushort
        || Value is int || Value is uint || Value is long || Value is ulong
        || Value is nint || Value is nuint
        || Value is float || Value is double || Value is decimal
    );

    public virtual bool Is<T>() => Value != null && Value is T;

    public double GetDouble()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException(Name);
        return double.Parse(value);
    }

    public virtual Task Set(object value)
    {
        if (IsInternal) _control.GetInternalData(Name).Value = value;
        else if (IsInput) _control.GetInput(Name).Value = value;
        else _control.GetOutput(Name).Value = value;
        return Task.CompletedTask;
    }
}
