using WELearning.Core.Shared.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockEventConnection
{
    public BlockEventConnection(string blockId, string eventName, EEventSource source)
    {
        BlockId = blockId;
        EventName = eventName;
        Source = source;
    }

    public string BlockId { get; set; }
    public string EventName { get; set; }
    public EEventSource Source { get; set; }
    public string SourceBlockId { get; set; }
    public string SourceEventName { get; set; }
}