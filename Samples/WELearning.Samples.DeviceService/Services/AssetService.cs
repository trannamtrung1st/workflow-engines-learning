using System.Text.Json;
using WELearning.Samples.DeviceService.Entities;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.Shared.Constants;
using WELearning.Samples.Shared.Models;
using WELearning.Samples.Shared.RabbitMq.Abstracts;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class AssetService : IAssetService
{
    private readonly DataStore _dataStore;
    private readonly IRateMonitor _monitoring;
    private readonly IRabbitMqConnectionManager _rabbitMqManager;

    public AssetService(
        DataStore dataStore,
        IRateMonitor monitoring,
        IRabbitMqConnectionManager rabbitMqManager)
    {
        _dataStore = dataStore;
        _monitoring = monitoring;
        _rabbitMqManager = rabbitMqManager;
    }

    public async Task AddMetricSeries(IEnumerable<MetricSeries> series, string demoBlockId)
    {
        const string MonitoringCategory = "Trigger incoming";

        await _dataStore.AddMetricSeries(series.Select(s => new MetricSeriesEntity(
            s.AssetId, s.AttributeName, s.Value, s.Timestamp
        )));

        foreach (var s in series)
        {
            // [NOTE] demo only
            if (s.AttributeName != "dynamic1")
                continue;
            Publish(
                topic: TopicNames.AttributeChanged,
                message: new AttributeChangedEvent(s.AssetId, s.AttributeName, s.Value, s.Timestamp, demoBlockId));
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

    public async Task<IEnumerable<AttributeSnapshot>> GetSnapshots(IEnumerable<string[]> assetAttributes)
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

    private void Publish(string topic, AttributeChangedEvent message)
    {
        var channel = _rabbitMqManager.GetChannel(channelId: TopicNames.AttributeChanged);
        ReadOnlyMemory<byte> bytes = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        channel.BasicPublish(
            exchange: topic,
            routingKey: "all",
            mandatory: true,
            basicProperties: properties,
            body: bytes);
    }
}
