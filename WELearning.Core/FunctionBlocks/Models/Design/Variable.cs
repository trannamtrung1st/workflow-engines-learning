using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable(string name, EDataType dataType, EVariableType variableType, object defaultValue = null)
    {
        Name = name;
        DataType = dataType;
        VariableType = variableType;
        DefaultValue = defaultValue;
    }

    public string Name { get; set; }
    public EDataType DataType { get; set; }
    public EVariableType VariableType { get; set; }
    public object DefaultValue { get; set; }

    public override string ToString() => $"{Name} ({VariableType})";

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Variable other)
            return false;

        return Name == other.Name && VariableType == other.VariableType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, VariableType);
    }
}