using WELearning.Core.Shared.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Variable
{
    public Variable(string name, EDataType dataType)
    {
        Name = name;
        DataType = dataType;
    }

    public string Name { get; set; }
    public EDataType DataType { get; set; }
}