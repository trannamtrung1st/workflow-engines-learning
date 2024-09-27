using System.Collections.Concurrent;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public abstract class BaseBlockDef
{
    public BaseBlockDef()
    {
    }

    public BaseBlockDef(string id, string name, ConcurrentDictionary<string, object> customData)
    {
        Id = id;
        Name = name;
        CustomData = customData;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string DefaultTriggerEvent { get; set; }
    public IEnumerable<BlockEvent> Events { get; set; }
    public IEnumerable<Variable> Variables { get; set; }
    private ConcurrentDictionary<string, object> _customData = [];
    public ConcurrentDictionary<string, object> CustomData
    {
        get => _customData; set
        {
            if (value is not null)
                _customData = value;
        }
    }
}
