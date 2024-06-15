namespace WELearning.Samples.DeviceService.Configurations;

public class AppSettings
{
    public int WorkerCount { get; set; }
    public int InitialConcurrencyLimit { get; set; }
    public int DevicesPerInterval { get; set; }
    public int SimulatorInterval { get; set; }
    public int LatencyMs { get; set; }

    public event EventHandler<IEnumerable<string>> Changed;

    public void InvokeChanged(IEnumerable<string> changes) => Changed?.Invoke(this, changes);
}