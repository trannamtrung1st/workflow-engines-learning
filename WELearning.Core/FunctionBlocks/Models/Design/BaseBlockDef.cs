namespace WELearning.Core.FunctionBlocks.Models.Design;

public abstract class BaseBlockDef
{
    public BaseBlockDef()
    {
        CustomData = [];
    }

    public BaseBlockDef(string id, string name, Dictionary<string, object> customData)
    {
        Id = id;
        Name = name;
        if (customData is not null)
            CustomData = customData;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string DefaultTriggerEvent { get; set; }
    public IEnumerable<BlockEvent> Events { get; set; }
    public IEnumerable<Variable> Variables { get; set; }
    public Dictionary<string, object> CustomData { get; }
}
