namespace WELearning.Samples.DeviceService.Configurations;

public class AppSettings
{
    public int WorkerCount { get; set; }
    public int ConcurrencyLimit { get; set; }
    public int DevicesPerInterval { get; set; }
    public int SimulatorInterval { get; set; }
    public int LatencyMs { get; set; }

    public event EventHandler Changed;

    public void InvokeChanged() => Changed?.Invoke(this, EventArgs.Empty);
}