namespace WELearning.Samples.Shared.Models;

public class AssetSnapshot
{
    public string AssetId { get; set; }
    public string AssetName { get; set; }
    public Dictionary<string, object> Snapshot { get; set; }
}