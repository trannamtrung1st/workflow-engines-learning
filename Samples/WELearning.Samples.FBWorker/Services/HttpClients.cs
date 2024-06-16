using WELearning.Samples.Shared.Constants;

namespace WELearning.Samples.FBWorker.Services;

public class HttpClients : IHttpClients
{
    public HttpClients(IHttpClientFactory httpClientFactory)
    {
        Device = httpClientFactory.CreateClient(ClientNames.DeviceService);
    }

    public HttpClient Device { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Device?.Dispose();
    }
}
