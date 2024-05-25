namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessVariableBinding
{
    public ProcessVariableBinding(string blockId, string variableName, object value)
    {
        BlockId = blockId;
        VariableName = variableName;
        Value = value;
    }

    public string BlockId { get; }
    public string VariableName { get; }
    public object Value { get; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not ProcessVariableBinding other)
            return false;

        return BlockId == other.BlockId && VariableName == other.VariableName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BlockId, VariableName);
    }
}