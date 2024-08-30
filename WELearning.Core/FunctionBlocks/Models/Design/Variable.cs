using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable() { }

    public Variable(string id,
        string name, EDataType dataType, EVariableType variableType,
        string objectType = null, object defaultValue = null)
    {
        Id = id;
        Name = name;
        DataType = dataType;
        VariableType = variableType;
        ObjectType = objectType;
        DefaultValue = defaultValue;
    }

    public Variable(
        string name, EDataType dataType, EVariableType variableType,
        string objectType = null, object defaultValue = null)
        : this(id: name, name, dataType, variableType, objectType, defaultValue)
    {
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public EDataType DataType { get; set; }
    public EVariableType VariableType { get; set; }
    public string ObjectType { get; set; }
    public object DefaultValue { get; set; }

    public override string ToString() => $"{Name} ({VariableType})";

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Variable other)
            return false;

        return Name == other.Name && VariableType == other.VariableType;
    }

    public bool CanInput() => VariableType == EVariableType.Input || CanInOut();
    public bool CanOutput() => VariableType == EVariableType.Output || CanInOut();
    public bool CanInOut() => VariableType == EVariableType.InOut || VariableType == EVariableType.Internal;

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, VariableType);
    }
}