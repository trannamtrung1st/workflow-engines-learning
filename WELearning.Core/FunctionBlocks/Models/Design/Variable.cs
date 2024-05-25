using WELearning.Core.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable(string name, EDataType dataType, object constantValue = null)
    {
        Name = name;
        DataType = dataType;
        ConstantValue = constantValue;
    }

    public string Name { get; set; }
    public EDataType DataType { get; set; }
    public object ConstantValue { get; set; }
}