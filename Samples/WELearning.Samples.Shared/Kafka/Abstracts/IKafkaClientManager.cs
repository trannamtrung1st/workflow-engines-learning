using Confluent.Kafka;

namespace WELearning.Samples.Shared.Kafka.Abstracts;

public interface IKafkaClientManager
{
    IProducer<TKey, TValue> GetProducer<TKey, TValue>(ProducerConfig config, string cacheKey = null);
    IConsumer<TKey, TValue> GetConsumer<TKey, TValue>(ConsumerConfig config, string cacheKey = null);
    IAdminClient GetAdminClient(AdminClientConfig config, string cacheKey = null);
}