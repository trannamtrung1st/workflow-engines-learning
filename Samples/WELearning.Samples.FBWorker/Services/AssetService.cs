using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.Services;

public class AssetService : IAssetService, IDisposable
{
    private readonly HttpClient _deviceClient;
    public AssetService(IHttpClientFactory httpClientFactory)
    {
        _deviceClient = httpClientFactory.CreateClient(ClientNames.DeviceService);
    }

    public async Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<string[]> assetAttributes, CancellationToken cancellationToken)
    {
        var response = await _deviceClient.PostAsJsonAsync(
            requestUri: $"/api/assets/attributes/snapshots",
            value: assetAttributes,
            cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();

        var snapshots = await response.Content.ReadFromJsonAsync<IEnumerable<AttributeSnapshot>>(cancellationToken: cancellationToken);
        return snapshots;
    }

    public async Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime, CancellationToken cancellationToken)
    {
        var queryBuilder = new QueryBuilder
        {
            { "beforeTime", beforeTime.ToString("s", CultureInfo.InvariantCulture) }
        };

        var series = await _deviceClient.GetFromJsonAsync<MetricSeries>(
            requestUri: $"/api/assets/{assetId}/attributes/{attributeName}/series{queryBuilder}",
            cancellationToken: cancellationToken);

        return series;
    }

    public async Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes, CancellationToken cancellationToken)
    {
        var response = await _deviceClient.PutAsJsonAsync(
            requestUri: $"/api/assets/attributes/snapshots",
            value: attributes,
            cancellationToken: cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _deviceClient?.Dispose();
    }
}
