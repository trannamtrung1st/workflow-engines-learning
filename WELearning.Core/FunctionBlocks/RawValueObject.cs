using WELearning.Core.Common.Extensions;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks;

public class RawValueObject : IValueObject, IDisposable
{
    private readonly ManualResetEventSlim _valueSet;

    public RawValueObject(Variable variable)
    {
        _valueSet = new ManualResetEventSlim();
        Variable = variable;
        ValueChanged = false;
        _value = variable.DefaultValue;
    }

    public RawValueObject(Variable variable, object value) : this(variable)
    {
        TrySetAndConvert(value);
    }

    public event EventHandler ValueSetEvent;

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
            SetValueSet();
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
            TrySetAndConvert(TempValue);
        else TrySetDefaultValue();
    }

    protected virtual void SetCoreValue(object value) => _value = value;

    protected virtual void SetValueSet()
    {
        lock (_valueSet) { _valueSet.Set(); }
        ValueSetEvent?.Invoke(this, EventArgs.Empty);
    }

    public virtual void WaitValueSet(CancellationToken cancellationToken) => _valueSet.Wait(cancellationToken);

    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public virtual bool IsNumeric => Value != null && (
        Value is sbyte || Value is byte || Value is short || Value is ushort
        || Value is int || Value is uint || Value is long || Value is ulong
        || Value is nint || Value is nuint
        || Value is float || Value is double || Value is decimal
    );

    public virtual bool IsRaw => true;

    public virtual void TrySetAndConvert(object value)
    {
        try { Value = value.As(Variable.DataType); }
        catch { Value = value; }
    }

    public override string ToString() => Value?.ToString();

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _valueSet.Dispose();
    }

    public virtual IValueObject CloneFor(Variable variable) => new RawValueObject(variable, value: Value);

    public bool RegisterTempValueSet(Func<Task> callback)
    {
        lock (_valueSet)
        {
            var registered = !ValueSet;
            if (registered)
            {
                void Handle(object o, EventArgs e)
                {
                    ValueSetEvent -= Handle;
                    _ = callback();
                }
                ValueSetEvent += Handle;
            }
            return registered;
        }
    }

    public virtual object GetProperty(string name) => throw new NotSupportedException($"Property {name} is not supported!");

    public virtual void TrySetDefaultValue()
    {
        if (!ValueSet && Variable.DefaultValue != null)
            TrySetAndConvert(Variable.DefaultValue);
    }
}
