using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockDataConnection
{
    public BlockDataConnection(string blockId, string variableName, string displayName, EDataSource source)
    {
        BlockId = blockId;
        VariableName = variableName;
        DisplayName = displayName;
        Source = source;
    }

    public string BlockId { get; set; }
    public string VariableName { get; set; }
    public string DisplayName { get; set; }
    public EDataSource Source { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceVariableName { get; set; }
    public object ConstantValue { get; set; }
}