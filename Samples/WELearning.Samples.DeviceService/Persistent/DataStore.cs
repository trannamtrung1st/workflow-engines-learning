using System.Collections.Concurrent;
using System.Text.Json;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Samples.DeviceService.Entities;
using WELearning.Samples.DeviceService.FunctionBlock.Basics;
using WELearning.Samples.DeviceService.FunctionBlock.Composites;

namespace WELearning.Samples.DeviceService.Persistent;

public class DataStore
{
    private const int DefaultNoOfAssets = 1000;
    private const int MaxSeriesCount = 3000;
    private readonly ConcurrentDictionary<string, AssetEntity> _assets;
    private readonly ConcurrentDictionary<(string, string), AssetAttributeEntity> _assetAttributes;
    private readonly MetricSeriesEntity[] _metricSeries;
    private readonly ConcurrentDictionary<string, string> _cfbDefinitions;
    private readonly ConcurrentDictionary<string, string> _bfbDefinitions;
    private readonly int _latencyMs;
    private readonly object _currentSeriesLock = new();
    private int _currentSeriesIdx;

    public DataStore(IConfiguration configuration)
    {
        _latencyMs = configuration.GetValue<int>("AppSettings:LatencyMs");
        _assets = new();
        _assetAttributes = new();
        _metricSeries = new MetricSeriesEntity[MaxSeriesCount];
        _cfbDefinitions = new();
        _bfbDefinitions = new();

        for (int i = 0; i < DefaultNoOfAssets; i++)
        {
            var assetId = $"asset-{i}";
            _assets.TryAdd(assetId, new(assetId, $"Asset {i}"));
            var dym1 = new AssetAttributeEntity(assetId, "dynamic1");
            var dym2 = new AssetAttributeEntity(assetId, "dynamic2");
            var sum = new AssetAttributeEntity(assetId, "sum");
            var prevSum = new AssetAttributeEntity(assetId, "prevSum");

            _assetAttributes.TryAdd(dym1.GetCompositePk(), dym1);
            _assetAttributes.TryAdd(dym2.GetCompositePk(), dym2);
            _assetAttributes.TryAdd(sum.GetCompositePk(), sum);
            _assetAttributes.TryAdd(prevSum.GetCompositePk(), prevSum);
        }

        _bfbDefinitions.TryAdd(PredefinedBFBs.AddJs.Id, JsonSerializer.Serialize(PredefinedBFBs.AddJs));
        _bfbDefinitions.TryAdd(PredefinedBFBs.MultiplyJs.Id, JsonSerializer.Serialize(PredefinedBFBs.MultiplyJs));
        // ... etc

        {
            var bLastSeriesBeforeDef = LastSeriesBeforeBFB.Build();
            var cfb = SumAttributesCFB.BuildIOBound(
                bLastSeriesBeforeId: bLastSeriesBeforeDef.Id, bAddId: PredefinedBFBs.AddJs.Id,
                out var bSumDef, out var bInputsDef, out var bOutputsDef
            );
            _cfbDefinitions.TryAdd(cfb.Id, JsonSerializer.Serialize(cfb));
            _bfbDefinitions.TryAdd(bLastSeriesBeforeDef.Id, JsonSerializer.Serialize(bLastSeriesBeforeDef));
            _bfbDefinitions.TryAdd(bSumDef.Id, JsonSerializer.Serialize(bSumDef));
            _bfbDefinitions.TryAdd(bInputsDef.Id, JsonSerializer.Serialize(bInputsDef));
            _bfbDefinitions.TryAdd(bOutputsDef.Id, JsonSerializer.Serialize(bOutputsDef));
        }

        {
            var cfb = SumAttributesCFB.BuildCpuBound(
                out var bMainDef, out var bInputsDef, out var bOutputsDef
            );
            _cfbDefinitions.TryAdd(cfb.Id, JsonSerializer.Serialize(cfb));
            _bfbDefinitions.TryAdd(bMainDef.Id, JsonSerializer.Serialize(bMainDef));
            _bfbDefinitions.TryAdd(bInputsDef.Id, JsonSerializer.Serialize(bInputsDef));
            _bfbDefinitions.TryAdd(bOutputsDef.Id, JsonSerializer.Serialize(bOutputsDef));
        }
    }

    public int NoOfAssets => _assets.Count;

    public async Task<AssetEntity> GetAsset(string assetId)
    {
        await Latency();
        var entity = _assets[assetId];
        return entity;
    }

    public async Task<IEnumerable<AssetAttributeEntity>> GetAttributes(string assetId)
    {
        await Latency();
        var entities = _assetAttributes.Values.Where(att => att.AssetId == assetId).ToList();
        return entities;
    }

    public async Task<IEnumerable<AssetAttributeEntity>> GetAttributes(IEnumerable<(string AssetId, string AttributeName)> assetAttributes)
    {
        await Latency();
        var entities = _assetAttributes
            .Where(att => assetAttributes.Contains(att.Key))
            .Select(att => att.Value)
            .ToList();
        return entities;
    }

    public async Task AddMetricSeries(IEnumerable<MetricSeriesEntity> series)
    {
        await Task.CompletedTask; // [NOTE] remove latency for stable publish rate
        foreach (var s in series)
        {
            int seriesIdx;
            lock (_currentSeriesLock)
            {
                seriesIdx = _currentSeriesIdx++;
                if (_currentSeriesIdx % MaxSeriesCount == 0)
                    _currentSeriesIdx = 0;
            }
            _metricSeries[_currentSeriesIdx] = s;

            var attribute = _assetAttributes[(s.AssetId, s.AttributeName)];
            attribute.Value = s.Value;
            attribute.Timestamp = s.Timestamp;
        }
    }

    public async Task<MetricSeriesEntity> LastSeriesBefore(string assetId, string attributeName, DateTime beforeTime)
    {
        await Latency();
        var series = _metricSeries
            .Where(s => s != null)
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefault(s => s.AssetId == assetId && s.AttributeName == attributeName && s.Timestamp < beforeTime);
        return series;
    }

    public async Task UpdateRuntime(IEnumerable<AssetAttributeEntity> attributes)
    {
        await Latency();
        foreach (var a in attributes)
            _assetAttributes[a.GetCompositePk()] = a;
    }

    public async Task<CompositeBlockDef> GetCfbDefinition(string id)
    {
        await Task.CompletedTask; // [NOTE] assuming this one is cached and optimized
        var defJson = _cfbDefinitions[id];
        var definition = JsonSerializer.Deserialize<CompositeBlockDef>(defJson);
        return definition;
    }

    public async Task<IEnumerable<BasicBlockDef>> GetBfbDefinitions(IEnumerable<string> ids)
    {
        await Task.CompletedTask; // [NOTE] assuming this one is cached and optimized
        var definitions = _bfbDefinitions
            .Where(d => ids.Contains(d.Key))
            .Select(d => JsonSerializer.Deserialize<BasicBlockDef>(d.Value))
            .ToArray();
        return definitions;
    }

    private Task Latency(int factor = 1) => Task.Delay(
        millisecondsDelay: Random.Shared.Next(maxValue: _latencyMs) * factor);
}