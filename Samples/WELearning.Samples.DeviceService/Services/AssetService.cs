using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WELearning.Samples.DeviceService.Entities;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.Shared.Models;
using WELearning.Shared.Diagnostic.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class AssetService : IAssetService, IDisposable
{
    private readonly DataStore _dataStore;
    private readonly IRateMonitor _monitoring;
    private readonly IOptionsSnapshot<ProducerConfig> _producerConfig;
    private readonly IProducer<Null, byte[]> _producer;

    public AssetService(
        DataStore dataStore,
        IRateMonitor monitoring,
        IOptionsSnapshot<ProducerConfig> producerConfig)
    {
        _dataStore = dataStore;
        _monitoring = monitoring;
        _producerConfig = producerConfig;
        _producer = GetProducer();
    }

    public async Task AddMetricSeries(IEnumerable<MetricSeries> series, string demoBlockId)
    {
        const string MonitoringCategory = "Trigger incoming";

        await _dataStore.AddMetricSeries(series.Select(s => new MetricSeriesEntity(
            s.AssetId, s.AttributeName, s.Value, s.Timestamp
        )));

        foreach (var s in series)
        {
            await Publish(
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

    private async Task Publish(string topic, AttributeChangedEvent message)
    {
        await _producer.ProduceAsync(topic, new Message<Null, byte[]>()
        {
            Value = JsonSerializer.SerializeToUtf8Bytes(message),
            Timestamp = new Timestamp(DateTime.UtcNow)
        });
    }

    private IProducer<Null, byte[]> GetProducer()
    {
        var builder = new ProducerBuilder<Null, byte[]>(_producerConfig.Value);
        return builder.Build();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _producer?.Dispose();
    }
}
