namespace WELearning.ConsoleApp.Testing.Entities;

public class MetricSeries : ICloneable
{
    public MetricSeries(string metric, double value, DateTime timestamp)
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