namespace WELearning.Shared.Concurrency.Configurations;

public class ResourceBasedConcurrencyScalingOptions
{
    public double IdealUsage { get; set; }
    public int ScaleFactor { get; set; }
    public int InitialConcurrencyLimit { get; set; }
    public int AcceptedQueueCount { get; set; }
    public double AcceptedAvailableConcurrency { get; set; }
    public double ScaleCheckInterval { get; set; }
    public int MovingAverageRange { get; set; }
    public double ConcurrencyCollectorInterval { get; set; }

    public event EventHandler<IEnumerable<string>> Changed;

    public void InvokeChanged(IEnumerable<string> changes) => Changed?.Invoke(this, changes);
}