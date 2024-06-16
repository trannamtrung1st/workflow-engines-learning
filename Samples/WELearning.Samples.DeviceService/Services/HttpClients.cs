using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.Shared.Constants;

namespace WELearning.Samples.DeviceService.Services;

public class HttpClients : IHttpClients
{
    public HttpClients(IHttpClientFactory httpClientFactory)
    {
        FBWorker = httpClientFactory.CreateClient(ClientNames.FBWorker);
    }

    public HttpClient FBWorker { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        FBWorker?.Dispose();
    }
}
