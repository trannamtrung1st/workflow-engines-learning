namespace WELearning.Core.FunctionBlocks.Models.Design;

public abstract class BaseBlockDef
{
    public BaseBlockDef(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string DefaultTriggerEvent { get; set; }
    public IEnumerable<BlockEvent> Events { get; set; }
    public IEnumerable<Variable> Variables { get; set; }
}
