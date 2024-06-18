namespace WELearning.Shared.Concurrency.Configurations;

public class RateLimiterOptions
{
    public int InitialLimit { get; set; }
    public ConcurrencyCollectorOptions CollectorOptions { get; set; }
}