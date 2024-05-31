namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockDataConnection
{
    public BlockDataConnection(string blockId, string variableName, string displayName)
    {
        BlockId = blockId;
        VariableName = variableName;
        DisplayName = displayName;
    }

    public string BlockId { get; set; }
    public string VariableName { get; set; }
    public string DisplayName { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceVariableName { get; set; }
}