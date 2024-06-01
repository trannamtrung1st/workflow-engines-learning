namespace WELearning.Core.Common;

public class ValueRef<T>
{
    private T _value;

    public ValueRef(T initialValue)
    {
        _value = initialValue;
    }

    public T Value { get => _value; set => _value = value; }
}