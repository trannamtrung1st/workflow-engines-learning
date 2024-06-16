namespace WELearning.Samples.Shared.Models;

public class AttributeChangedEvent
{
    public AttributeChangedEvent(string assetId, string attributeName, double value, DateTime timestamp, string demoBlockId)
    {
        AssetId = assetId;
        AttributeName = attributeName;
        Value = value;
        Timestamp = timestamp;
        DemoBlockId = demoBlockId;
    }

    public string AssetId { get; set; }
    public string AttributeName { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string DemoBlockId { get; set; }
}