using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockReference : BlockConnection
{
    public BlockReference(string blockId, string variableName, string displayName, EBindingType bindingType) : base(blockId, variableName, displayName)
    {
        BindingType = bindingType;
    }

    public EBindingType BindingType { get; set; }
}