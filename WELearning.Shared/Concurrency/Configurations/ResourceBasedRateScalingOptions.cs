namespace WELearning.Shared.Concurrency.Configurations;

public class ResourceBasedRateScalingOptions
{
    public int InitialLimit { get; set; }
    public double IdealUsage { get; set; }
    public int ScaleFactor { get; set; }
    public int AcceptedQueueCount { get; set; }
    public double AcceptedAvailablePercentage { get; set; }
    public double ScaleCheckInterval { get; set; }
}