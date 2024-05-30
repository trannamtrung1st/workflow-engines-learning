using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockDataConnection
{
    public BlockDataConnection(
        string blockId, string variableName, string displayName,
        EBindingType variableType, EDataSource source)
    {
        if (
            (variableType == EBindingType.Output && source != EDataSource.External)
            || (variableType != EBindingType.Input && variableType != EBindingType.Output)
        ) throw new ArgumentException("Invalid variable type!");

        BlockId = blockId;
        VariableName = variableName;
        DisplayName = displayName;
        VariableType = variableType;
        Source = source;
    }

    public string BlockId { get; set; }
    public string VariableName { get; set; }
    public string DisplayName { get; set; }
    public EBindingType VariableType { get; set; }
    public EDataSource Source { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceVariableName { get; set; }
}