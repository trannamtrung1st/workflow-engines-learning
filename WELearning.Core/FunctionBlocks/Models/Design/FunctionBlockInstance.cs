namespace WELearning.Core.FunctionBlocks.Models.Design;

public class FunctionBlockInstance
{
    public FunctionBlockInstance(FunctionBlock definition, string id = null, string displayName = null)
    {
        Id = id ?? definition.Id;
        DisplayName = displayName ?? definition.Name;
        Definition = definition;
    }

    public string Id { get; set; }
    public string DisplayName { get; set; }
    public FunctionBlock Definition { get; set; }
}