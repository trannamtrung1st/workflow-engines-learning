using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable(string name, EDataType dataType, EBindingType bindingType, object defaultValue = null)
    {
        Name = name;
        DataType = dataType;
        BindingType = bindingType;
        DefaultValue = defaultValue;
    }

    public string Name { get; set; }
    public EDataType DataType { get; set; }
    public EBindingType BindingType { get; set; }
    public object DefaultValue { get; set; }

    public override string ToString() => $"{Name} ({BindingType})";

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not Variable other)
            return false;

        return Name == other.Name && BindingType == other.BindingType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, BindingType);
    }
}