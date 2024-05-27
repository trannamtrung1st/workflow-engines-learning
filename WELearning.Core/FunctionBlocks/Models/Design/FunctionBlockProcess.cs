using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class FunctionBlockProcess
{
    public FunctionBlockProcess(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; }
    public string Name { get; set; }

    public IEnumerable<string> DefaultBlockIds { get; set; }
    public IEnumerable<FunctionBlock> Blocks { get; set; }

    public IEnumerable<BlockEventConnection> EventConnections { get; set; }
    public IEnumerable<BlockDataConnection> DataConnections { get; set; }

    public virtual IEnumerable<BlockTrigger> FindNextBlocks(
        string sourceBlockId,
        IEnumerable<string> outputEvents)
    {
        var nextBlockTriggers = EventConnections
            .Where(c => c.Source == EEventSource.Internal && c.SourceBlockId == sourceBlockId && outputEvents.Contains(c.SourceEventName))
            .Select(c => new BlockTrigger(blockId: c.BlockId, triggerEvent: c.EventName));
        return nextBlockTriggers;
    }
}