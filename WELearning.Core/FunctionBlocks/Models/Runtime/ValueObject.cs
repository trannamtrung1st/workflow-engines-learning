using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ValueObject : IDisposable
{
    private readonly ManualResetEventSlim _valueSet;

    public ValueObject(Variable variable)
    {
        _valueSet = new ManualResetEventSlim();
        if (variable.DefaultValue != null)
            Value = variable.DefaultValue;
        Variable = variable;
        ValueChanged = false;
    }

    public ValueObject(Variable variable, object value) : this(variable)
    {
        Value = value;
    }

    public Variable Variable { get; }
    public bool ValueChanged { get; private set; }
    public bool ValueSet => _valueSet.IsSet;
    private object _value;
    public object Value
    {
        get => _value; set
        {
            // [TODO] refreshed output
            ValueChanged = true;
            _value = value;
            _valueSet.Set();
        }
    }

    public void WaitValueSet(CancellationToken cancellationToken) => _valueSet.Wait(cancellationToken);

    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public virtual bool IsNumeric => Value != null && (
        Value is sbyte || Value is byte || Value is short || Value is ushort
        || Value is int || Value is uint || Value is long || Value is ulong
        || Value is nint || Value is nuint
        || Value is float || Value is double || Value is decimal
    );

    public virtual bool Is<T>() => Value != null && Value is T;

    public double ToDouble()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException();
        return double.Parse(value);
    }

    public int ToInt()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException();
        return int.Parse(value);
    }

    public override string ToString() => Value?.ToString();

    public void Dispose() => _valueSet.Dispose();
}