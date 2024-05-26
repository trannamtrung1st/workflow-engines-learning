using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockBinding : IBlockBinding
{
    private readonly BlockExecutionControl _control;
    public BlockBinding(string name, BlockExecutionControl control)
    {
        _control = control;
        Name = name;
        if (control.InputSnapshot.TryGetValue(name, out var inputValue))
        {
            Exists = true; IsInput = true; IsOutput = false;
            Value = inputValue;
        }
        else if (control.OutputSnapshot.TryGetValue(name, out var outputValue))
        {
            Exists = true; IsInput = false; IsOutput = true;
            Value = outputValue;
        }
        else
            Exists = false; IsInput = false; IsOutput = true;
    }

    public virtual string Name { get; protected set; }
    public virtual object Value { get; protected set; }
    public virtual bool Exists { get; protected set; }
    public virtual bool IsInput { get; protected set; }
    public virtual bool IsOutput { get; protected set; }

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
        if (IsInput) _control.InputSnapshot[Name] = value;
        else _control.OutputSnapshot[Name] = value;
        return Task.CompletedTask;
    }
}
