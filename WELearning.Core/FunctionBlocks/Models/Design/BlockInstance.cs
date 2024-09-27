namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockInstance
{
    public BlockInstance()
    {
    }

    public BlockInstance(string definitionId, string id = null, string displayName = null, Dictionary<string, object> customData = null) : this()
    {
        Id = id ?? definitionId;
        DisplayName = displayName ?? Id;
        DefinitionId = definitionId;
        CustomData = customData;
    }

    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string DefinitionId { get; set; }
    private Dictionary<string, object> _customData = [];
    public Dictionary<string, object> CustomData
    {
        get => _customData; set
        {
            if (value is not null)
                _customData = value;
        }
    }
}
