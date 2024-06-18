namespace WELearning.Shared.Concurrency.Configurations;

public class ResourceBasedConcurrencyScalingOptions
{
    public double IdealUsage { get; set; }
    public int ScaleFactor { get; set; }
    public int InitialLimit { get; set; }
    public int AcceptedQueueCount { get; set; }
    public double AcceptedAvailableConcurrency { get; set; }
    public double ScaleCheckInterval { get; set; }
    public int MovingAverageRange { get; set; }
    public double ConcurrencyCollectorInterval { get; set; }
}