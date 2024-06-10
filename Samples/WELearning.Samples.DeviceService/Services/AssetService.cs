using WELearning.Samples.DeviceService.Entities;
using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class AssetService : IAssetService
{
    private readonly DataStore _dataStore;
    private readonly IMessageQueue _messageQueue;
    private readonly IMonitoring _monitoring;

    public AssetService(
        DataStore dataStore,
        IMessageQueue messageQueue,
        IMonitoring monitoring)
    {
        _dataStore = dataStore;
        _messageQueue = messageQueue;
        _monitoring = monitoring;
    }

    public async Task AddMetricSeries(IEnumerable<MetricSeries> series)
    {
        const string MonitoringCategory = "Series incoming";

        await _dataStore.AddMetricSeries(series.Select(s => new MetricSeriesEntity(
            s.AssetId, s.AttributeName, s.Value, s.Timestamp
        )));

        foreach (var s in series)
        {
            await _messageQueue.Publish(TopicNames.AttributeChanged, new AttributeChangedEvent
            {
                AssetId = s.AssetId,
                AttributeName = s.AttributeName,
                Timestamp = s.Timestamp,
                Value = s.Value
            });
        }

        _monitoring.Capture(category: MonitoringCategory, count: 1);
    }

    public async Task<AssetSnapshot> GetAssetSnapshot(string assetId)
    {
        var asset = await _dataStore.GetAsset(assetId);
        var snapshot = new Dictionary<string, object>();
        var assetAttributes = await _dataStore.GetAttributes(assetId);
        foreach (var att in assetAttributes)
            snapshot[att.AttributeName] = att.Value;

        var assetSnapshot = new AssetSnapshot()
        {
            AssetId = asset.AssetId,
            AssetName = asset.AssetName,
            Snapshot = snapshot
        };
        return assetSnapshot;
    }

    public async Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<(string AssetId, string AttributeName)> assetAttributes)
    {
        var attributes = await _dataStore.GetAttributes(assetAttributes);
        var snapshots = attributes.Select(a => new AttributeSnapshot
        {
            AssetId = a.AssetId,
            AttributeName = a.AttributeName,
            Timestamp = a.Timestamp,
            Value = a.Value
        }).ToArray();
        return snapshots;
    }

    public async Task<MetricSeries> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime)
    {
        var series = await _dataStore.LastSeriesBefore(assetId, attributeName, beforeTime);
        return series == null
            ? null : new MetricSeries(series.AssetId, series.AttributeName, series.Value, series.Timestamp);
    }

    public async Task UpdateRuntime(IEnumerable<AttributeSnapshot> attributes)
    {
        var entities = attributes.Select(a => new AssetAttributeEntity(a.AssetId, a.AttributeName)
        {
            Value = a.Value,
            Timestamp = a.Timestamp
        }).ToArray();

        await _dataStore.UpdateRuntime(entities);
    }
}
