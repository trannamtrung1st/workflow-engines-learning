namespace WELearning.Samples.DeviceService.Entities;

public class MetricSeriesEntity
{
    public MetricSeriesEntity(string assetId, string attributeName, double value, DateTime timestamp)
    {
        AssetId = assetId;
        AttributeName = attributeName;
        Value = value;
        Timestamp = timestamp;
    }

    public string AssetId { get; set; }
    public string AttributeName { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}