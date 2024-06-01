using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class VariableBinding
{
    public VariableBinding(string variableName, object value, EBindingType type)
    {
        VariableName = variableName;
        Value = value;
        Type = type;
    }

    public VariableBinding(string variableName, IValueObject valueObject, EBindingType type)
    {
        VariableName = variableName;
        ValueObject = valueObject;
        Type = type;
    }

    public EBindingType Type { get; }
    public string VariableName { get; }
    public object Value { get; }
    public IValueObject ValueObject { get; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not VariableBinding other)
            return false;

        return VariableName == other.VariableName && Type == other.Type;
    }

    public override int GetHashCode() => HashCode.Combine(VariableName, Type);

    public override string ToString() => (Value ?? ValueObject?.Value).ToString();
}
