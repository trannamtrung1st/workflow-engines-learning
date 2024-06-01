namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockEventConnection
{
    public BlockEventConnection(string blockId, string eventName)
    {
        BlockId = blockId;
        EventName = eventName;
    }

    public string BlockId { get; set; }
    public string EventName { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceEventName { get; set; }
}