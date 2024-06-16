namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IHttpClients : IDisposable
{
    HttpClient FBWorker { get; }
}