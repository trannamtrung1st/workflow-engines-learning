using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockDataConnection
{
    public BlockDataConnection(string blockId, string variableName, EDataSource source)
    {
        BlockId = blockId;
        VariableName = variableName;
        Source = source;
    }

    public string BlockId { get; set; }
    public string VariableName { get; set; }
    public EDataSource Source { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceVariableName { get; set; }
    public Variable ConstantVariable { get; set; }
}