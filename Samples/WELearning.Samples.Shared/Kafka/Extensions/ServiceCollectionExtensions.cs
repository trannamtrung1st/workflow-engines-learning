using Microsoft.Extensions.DependencyInjection;
using WELearning.Samples.Shared.Kafka;
using WELearning.Samples.Shared.Kafka.Abstracts;

namespace WELearning.Samples.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaClientManager(this IServiceCollection services)
    {
        return services.AddSingleton<IKafkaClientManager, KafkaClientManager>();
    }
}
