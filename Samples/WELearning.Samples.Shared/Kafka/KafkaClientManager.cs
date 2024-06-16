using System.Collections.Concurrent;
using Confluent.Kafka;
using WELearning.Samples.Shared.Kafka.Abstracts;

namespace WELearning.Samples.Shared.Kafka;

public class KafkaClientManager : IKafkaClientManager, IDisposable
{
    private readonly ConcurrentDictionary<string, object> _producerMap;
    private readonly ConcurrentDictionary<string, object> _consumerMap;
    private readonly ConcurrentDictionary<string, IAdminClient> _adminClientMap;

    public KafkaClientManager()
    {
        _producerMap = new();
        _consumerMap = new();
        _adminClientMap = new();
    }

    public IAdminClient GetAdminClient(AdminClientConfig config, string cacheKey = null)
    {
        IAdminClient CreateClient() => new AdminClientBuilder(config).Build();

        return cacheKey != null
            ? _adminClientMap.GetOrAdd(cacheKey, (_) => CreateClient())
            : CreateClient();
    }

    public IConsumer<TKey, TValue> GetConsumer<TKey, TValue>(ConsumerConfig config, string cacheKey = null)
    {
        IConsumer<TKey, TValue> CreateClient() => new ConsumerBuilder<TKey, TValue>(config).Build();

        return cacheKey != null
            ? _consumerMap.GetOrAdd(cacheKey, (_) => CreateClient()) as IConsumer<TKey, TValue>
            : CreateClient();
    }

    public IProducer<TKey, TValue> GetProducer<TKey, TValue>(ProducerConfig config, string cacheKey = null)
    {
        IProducer<TKey, TValue> CreateClient() => new ProducerBuilder<TKey, TValue>(config).Build();

        return cacheKey != null
            ? _producerMap.GetOrAdd(cacheKey, (_) => CreateClient()) as IProducer<TKey, TValue>
            : CreateClient();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var client in _producerMap.Values)
            (client as IDisposable)?.Dispose();
        foreach (var client in _consumerMap.Values)
            (client as IDisposable)?.Dispose();
        foreach (var client in _adminClientMap.Values)
            (client as IDisposable)?.Dispose();
    }

}