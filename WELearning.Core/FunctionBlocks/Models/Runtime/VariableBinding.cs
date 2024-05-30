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

    public EBindingType Type { get; }
    public string VariableName { get; }
    public object Value { get; private set; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not VariableBinding other)
            return false;

        return VariableName == other.VariableName && Type == other.Type;
    }

    public override int GetHashCode() => HashCode.Combine(VariableName, Type);

    public override string ToString() => Value?.ToString();
}
