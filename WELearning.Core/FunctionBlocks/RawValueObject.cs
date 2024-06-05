using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks;

public class RawValueObject : IValueObject, IDisposable
{
    protected readonly ManualResetEventSlim _valueSet;

    public RawValueObject(Variable variable)
    {
        _valueSet = new ManualResetEventSlim();
        if (variable.DefaultValue != null)
            Value = variable.DefaultValue;
        Variable = variable;
        ValueChanged = false;
    }

    public RawValueObject(Variable variable, object value) : this(variable)
    {
        Value = value;
    }

    public Variable Variable { get; }
    public virtual bool ValueChanged { get; protected set; }
    public virtual bool ValueSet => _valueSet.IsSet;
    private object _value;
    public virtual object Value
    {
        get => _value; set
        {
            ValueChanged = true;
            SetCoreValue(value);
            _tempValue = null;
            _tempValueSet = false;
            _valueSet.Set();
        }
    }

    private bool _tempValueSet;
    public virtual bool TempValueSet => _tempValueSet;
    private object _tempValue;
    public virtual object TempValue
    {
        get => _tempValue; set
        {
            _tempValue = value;
            _tempValueSet = true;
        }
    }

    public virtual void TryCommit()
    {
        if (_tempValueSet)
            Value = TempValue;
    }

    protected virtual void SetCoreValue(object value) => _value = value;

    public virtual void WaitValueSet(CancellationToken cancellationToken) => _valueSet.Wait(cancellationToken);

    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public virtual bool IsNumeric => Value != null && (
        Value is sbyte || Value is byte || Value is short || Value is ushort
        || Value is int || Value is uint || Value is long || Value is ulong
        || Value is nint || Value is nuint
        || Value is float || Value is double || Value is decimal
    );

    public virtual bool IsRaw => true;

    public virtual double AsDouble()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException();
        return double.Parse(value);
    }

    public virtual int AsInt()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException();
        return int.Parse(value);
    }

    public virtual bool AsBool()
    {
        var value = Value?.ToString();
        if (value == null) throw new ArgumentNullException();
        return bool.Parse(value);
    }

    public override string ToString() => Value?.ToString();

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _valueSet.Dispose();
    }

    public virtual object As(EDataType dataType)
    {
        switch (dataType)
        {
            case EDataType.Bool: return AsBool();
            case EDataType.Int: return AsInt();
            case EDataType.DateTime: return (DateTime)Value;
            case EDataType.Double: return AsDouble();
            case EDataType.Numeric: return AsDouble();
            case EDataType.Object: return Value;
            case EDataType.String: return Value?.ToString();
            default: throw new NotSupportedException($"Data type {dataType} is not supported for this value!");
        }
    }

    public virtual IValueObject CloneFor(Variable variable) => new RawValueObject(variable, value: Value);
}