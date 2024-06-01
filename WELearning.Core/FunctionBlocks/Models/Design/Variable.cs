using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable(string name, EDataType dataType, EVariableType variableType, string detailedType = null, object defaultValue = null)
    {
        Name = name;
        DataType = dataType;
        VariableType = variableType;
        DetailedType = detailedType;
        DefaultValue = defaultValue;
    }

    public string Name { get; set; }
    public EDataType DataType { get; set; }
    public EVariableType VariableType { get; set; }
    public string DetailedType { get; set; }
    public object DefaultValue { get; set; }

    public override string ToString() => $"{Name} ({VariableType})";

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Variable other)
            return false;

        return Name == other.Name && VariableType == other.VariableType;
    }

    public bool CanInput() => VariableType == EVariableType.Input || VariableType == EVariableType.InOut;
    public bool CanOutput() => VariableType == EVariableType.Output || VariableType == EVariableType.InOut;

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, VariableType);
    }
}