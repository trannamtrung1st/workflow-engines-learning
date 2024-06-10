namespace WELearning.Samples.DeviceService.Models;

public class MetricSeries
{
    public MetricSeries()
    {
    }

    public MetricSeries(string assetId, string attributeName, double value, DateTime? timestamp = null) : this()
    {
        AssetId = assetId;
        AttributeName = attributeName;
        Value = value;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    public string AssetId { get; set; }
    public string AttributeName { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}