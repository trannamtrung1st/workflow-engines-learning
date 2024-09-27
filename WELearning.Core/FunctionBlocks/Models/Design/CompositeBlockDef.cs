using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class CompositeBlockDef : BaseBlockDef
{
    private Dictionary<string, BaseBlockDef> _definitionMap;

    public CompositeBlockDef(string id, string name, Dictionary<string, object> customData = null) : base(id, name, customData)
    {
    }

    public IEnumerable<BlockInstance> Blocks { get; set; }
    public IEnumerable<BlockEventConnection> EventConnections { get; set; }
    public IEnumerable<BlockConnection> DataConnections { get; set; }
    public IEnumerable<BlockConnection> References { get; set; }

    public BaseBlockDef GetDefinition(string definitionId)
    {
        if (_definitionMap == null) throw new InvalidOperationException("Defintions are not yet mapped!");
        return _definitionMap[definitionId];
    }

    public void MapDefinitions(IEnumerable<BaseBlockDef> definitions)
        => _definitionMap = definitions.ToDictionary(x => x.Id, x => x);

    public virtual IEnumerable<BlockTrigger> FindNextBlocks(
        string sourceBlockId,
        IEnumerable<string> outputEvents)
    {
        var nextBlockTriggers = EventConnections
            .Where(c => c.SourceBlockId == sourceBlockId && outputEvents.Contains(c.SourceEventName))
            .Select(c => new BlockTrigger(blockId: c.BlockId, triggerEvent: c.EventName));
        return nextBlockTriggers;
    }

    public virtual IEnumerable<BlockTrigger> FindNextBlocks(string triggerEvent)
    {
        var nextBlockTriggers = EventConnections
            .Where(c => c.SourceEventName == triggerEvent)
            .Select(c => new BlockTrigger(blockId: c.BlockId, triggerEvent: c.EventName));
        return nextBlockTriggers;
    }
}
