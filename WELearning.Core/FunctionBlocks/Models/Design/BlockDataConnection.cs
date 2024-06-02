using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockDataConnection : BlockConnection
{
    public BlockDataConnection(string blockId, string variableName, string displayName, EBindingType bindingType) : base(blockId, variableName, displayName)
    {
        BindingType = bindingType;
    }

    public EBindingType BindingType { get; set; }
    public string SourceBlockId { get; set; }
}
