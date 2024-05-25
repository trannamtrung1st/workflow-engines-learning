namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockTrigger
{
    public BlockTrigger(string blockId, string triggerEvent)
    {
        BlockId = blockId;
        TriggerEvent = triggerEvent;
    }

    public string BlockId { get; }
    public string TriggerEvent { get; }

    public override bool Equals(object obj)
    {
        if (obj == null || obj is not BlockTrigger other)
            return false;

        return BlockId == other.BlockId && TriggerEvent == other.TriggerEvent;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BlockId, TriggerEvent);
    }
}