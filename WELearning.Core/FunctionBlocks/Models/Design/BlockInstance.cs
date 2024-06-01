namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockInstance
{
    public BlockInstance(string definitionId, string id = null, string displayName = null)
    {
        Id = id ?? definitionId;
        DisplayName = displayName ?? Id;
        DefinitionId = definitionId;
    }

    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string DefinitionId { get; set; }
}