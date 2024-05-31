namespace WELearning.Core.FunctionBlocks.Models.Design;

public class FunctionBlockInstance
{
    public FunctionBlockInstance(FunctionBlock definition, string id = null, string displayName = null)
    {
        Id = id ?? definition.Id;
        DisplayName = displayName ?? id ?? definition.Name;
        Definition = definition;
    }

    public string Id { get; set; }
    public string DisplayName { get; set; }

    // [TODO] refactor definition
    public FunctionBlock Definition { get; set; }
}