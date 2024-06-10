namespace WELearning.Samples.DeviceService.Models;

public class AttributeChangedEvent
{
    public string AssetId { get; set; }
    public string AttributeName { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}