namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessVariableBinding
{
    public ProcessVariableBinding(string blockId, VariableBinding binding)
    {
        BlockId = blockId;
        Binding = binding;
    }

    public string BlockId { get; }
    public VariableBinding Binding { get; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not ProcessVariableBinding other)
            return false;

        return BlockId == other.BlockId && Binding == other.Binding;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BlockId, Binding);
    }
}
