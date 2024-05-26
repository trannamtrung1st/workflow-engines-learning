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
    }

    public string Name { get; }
    public virtual object Value { get; protected set; }
    public bool Exists { get; }
    public bool IsInput { get; }
    public bool IsOutput { get; }

    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public bool IsNumeric => Value != null && (
        Value is sbyte || Value is byte || Value is short || Value is ushort
        || Value is int || Value is uint || Value is long || Value is ulong
        || Value is nint || Value is nuint
        || Value is float || Value is double || Value is decimal
    );

    public bool Is<T>() => Value != null && Value is T;

    public virtual Task Set(object value)
    {
        Value = value;
        if (IsInput)
            _control.InputSnapshot[Name] = value;
        else if (IsOutput)
            _control.OutputSnapshot[Name] = value;
        return Task.CompletedTask;
    }
}
