namespace WELearning.Samples.DeviceService.Entities;

public class AssetEntity
{
    public AssetEntity(string assetId, string assetName)
    {
        AssetId = assetId;
        AssetName = assetName;
    }

    public string AssetId { get; set; }
    public string AssetName { get; set; }
}
