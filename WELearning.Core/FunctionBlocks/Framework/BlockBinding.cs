using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockBinding : IBlockBinding
{
    private readonly BlockExecutionControl _control;
    public BlockBinding(string name, BlockExecutionControl control, bool isInternal)
    {
        _control = control;
        Name = name;
        if (isInternal)
        {
            IsInput = false; IsOutput = false; IsInternal = true;
            if (control.InternalDataSnapshot.TryGetValue(name, out var internalValue))
            {
                Exists = true;
                Value = internalValue;
            }
        }
        else if (control.InputSnapshot.TryGetValue(name, out var inputValue))
        {
            Exists = true; IsInput = true; IsOutput = false; IsInternal = false;
            Value = inputValue;
        }
        else if (control.OutputSnapshot.TryGetValue(name, out var outputValue))
        {
            Exists = true; IsInput = false; IsOutput = true; IsInternal = false;
            Value = outputValue;
        }
        else
        {
            Exists = false; IsInput = false; IsOutput = true; IsInternal = false;
        }
    }

    public virtual string Name { get; protected set; }
    public virtual object Value { get; protected set; }
    public virtual bool Exists { get; protected set; }
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
        Value = value;
        Exists = true;
        if (IsInternal) _control.InternalDataSnapshot[Name] = value;
        else if (IsInput) _control.InputSnapshot[Name] = value;
        else _control.OutputSnapshot[Name] = value;
        return Task.CompletedTask;
    }
}
