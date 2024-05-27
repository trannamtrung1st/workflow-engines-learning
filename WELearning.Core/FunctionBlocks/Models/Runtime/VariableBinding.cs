namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class VariableBinding
{
    public VariableBinding(string variableName)
    {
        VariableName = variableName;
        _valueSet = new ManualResetEventSlim();
    }

    public VariableBinding(string variableName, object value) : this(variableName)
    {
        Value = value;
    }

    public string VariableName { get; }
    private ManualResetEventSlim _valueSet;
    public bool ValueSet => _valueSet.IsSet;
    private object _value;
    public object Value
    {
        get => _value; set
        {
            _value = value;
            _valueSet.Set();
        }
    }

    public void WaitValueSet(CancellationToken cancellationToken) => _valueSet.Wait(cancellationToken);

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not VariableBinding other)
            return false;

        return VariableName == other.VariableName;
    }

    public override int GetHashCode() => VariableName.GetHashCode();

    public override string ToString() => Value?.ToString();
}