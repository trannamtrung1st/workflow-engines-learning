namespace WELearning.Samples.FBWorker.Configurations;

public class AppSettings
{
    public int WorkerCount { get; set; }

    public event EventHandler<IEnumerable<string>> Changed;

    public void InvokeChanged(IEnumerable<string> changes) => Changed?.Invoke(this, changes);
}