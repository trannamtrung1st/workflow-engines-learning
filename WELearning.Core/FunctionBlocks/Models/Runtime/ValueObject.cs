namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ValueObject
{
    public ValueObject()
    {
        ValueSet = new ManualResetEventSlim();
    }

    private object _value;
    public object Value
    {
        get => _value; set
        {
            _value = value;
            ValueSet.Set();
        }
    }

    public ManualResetEventSlim ValueSet { get; }

    public override string ToString() => Value?.ToString();
}