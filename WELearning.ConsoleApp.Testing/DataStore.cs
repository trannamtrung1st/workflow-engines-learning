using WELearning.ConsoleApp.Testing.Entities;

public class DataStore
{
    private readonly Dictionary<string, EntryEntity> _entryMap;
    private readonly Dictionary<string, MetricSnapshot> _snapshotMap;
    private readonly List<MetricSeries> _series;

    public DataStore()
    {
        _entryMap = new();
        _entryMap["Temperature"] = new("Temperature", Random.Shared.NextDouble() * 50);
        _entryMap["Humidity"] = new("Humidity", Random.Shared.NextDouble() * 50);
        _entryMap["Report"] = new("Report", null);
        _entryMap["FinalReport"] = new("FinalReport", null);
        _entryMap["FinalPrefix"] = new("FinalPrefix", "FINAL: ");

        _snapshotMap = new();
        var latestTs = DateTime.Parse("2024-01-01 10:00:00");
        latestTs = DateTime.SpecifyKind(latestTs, DateTimeKind.Utc);
        var sampleMetric = MetricSnapshot.SampleMetric;
        _snapshotMap[sampleMetric] = new(metric: sampleMetric, value: 10, timestamp: latestTs);

        _series = new();
        _series.Add(new(metric: sampleMetric, value: 10, timestamp: latestTs));
        _series.Add(new(metric: sampleMetric, value: 9, timestamp: latestTs.AddHours(-1)));
        _series.Add(new(metric: sampleMetric, value: 8, timestamp: latestTs.AddHours(-2)));
        _series.Add(new(metric: sampleMetric, value: 7, timestamp: latestTs.AddHours(-3)));
        _series.Add(new(metric: sampleMetric, value: 6, timestamp: latestTs.AddHours(-4)));
        _series.Add(new(metric: sampleMetric, value: 5, timestamp: latestTs.AddHours(-5)));
        _series.Add(new(metric: sampleMetric, value: 4, timestamp: latestTs.AddHours(-6)));
        _series.Add(new(metric: sampleMetric, value: 3, timestamp: latestTs.AddHours(-7)));
    }

    public EntryEntity GetEntry(string key) => _entryMap[key].Clone() as EntryEntity;

    public void UpdateEntry(string key, object value) => _entryMap[key].Value = value;

    public MetricSnapshot GetMetricSnapshot(string metric) => _snapshotMap[metric].Clone() as MetricSnapshot;
    public MetricSeries LastSeriesBefore(string metric, DateTime beforeTime) =>
        _series.OrderByDescending(s => s.Timestamp)
            .Where(s => s.Metric == metric && s.Timestamp < beforeTime)
            .FirstOrDefault();
}