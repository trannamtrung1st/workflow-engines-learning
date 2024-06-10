namespace WELearning.Samples.DeviceService.Entities;

public class AssetAttributeEntity
{
    public AssetAttributeEntity(string assetId, string attributeName)
    {
        AssetId = assetId;
        AttributeName = attributeName;
    }

    public string AssetId { get; set; }
    public string AttributeName { get; set; }
    public double? Value { get; set; }
    public DateTime? Timestamp { get; set; }

    public (string, string) GetCompositePk() => (AssetId, AttributeName);
}
