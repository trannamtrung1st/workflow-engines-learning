using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockConnection
{
    public BlockConnection(string blockId, string variableName, string displayName, EBindingType bindingType)
    {
        BlockId = blockId;
        VariableName = variableName;
        DisplayName = displayName;
        BindingType = bindingType;
    }

    public string BlockId { get; set; }
    public string VariableName { get; set; }
    public string DisplayName { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceVariableName { get; set; }
    public EBindingType BindingType { get; set; }
}
