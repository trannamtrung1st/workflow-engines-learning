namespace WELearning.ConsoleApp.Testing.Entities;

public class MetricSnapshot : ICloneable
{
    public const string SampleMetric = "Temperature";

    public MetricSnapshot(string metric, double value, DateTime timestamp)
    {
        Metric = metric;
        Value = value;
        Timestamp = timestamp;
    }

    public string Metric { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public override string ToString() => $"{Metric}: {Value}";
}