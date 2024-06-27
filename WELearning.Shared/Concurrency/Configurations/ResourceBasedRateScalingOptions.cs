namespace WELearning.Shared.Concurrency.Configurations;

public class ResourceBasedRateScalingOptions
{
    public double ScaleCheckInterval { get; set; }
    public IDictionary<string, ScalingParameters> Parameters { get; set; }
    public RateCollectorOptions RateCollectorOptions { get; set; }
}

public class ScalingParameters
{
    public double IdealUsage { get; set; }
    public int ScaleFactor { get; set; }
    public int AcceptedQueueCount { get; set; }
    public double AcceptedAvailablePercentage { get; set; }
}