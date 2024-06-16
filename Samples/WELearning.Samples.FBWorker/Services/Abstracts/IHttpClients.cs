namespace WELearning.Samples.FBWorker.Services;

public interface IHttpClients : IDisposable
{
    HttpClient Device { get; }
}