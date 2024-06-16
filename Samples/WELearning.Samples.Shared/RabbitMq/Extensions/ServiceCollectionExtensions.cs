using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using WELearning.Samples.Shared.RabbitMq;
using WELearning.Samples.Shared.RabbitMq.Abstracts;

namespace WELearning.Samples.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqConnectionManager(this IServiceCollection services,
        ConnectionFactory connectionFactory,
        Func<IServiceProvider, Action<IConnection>> configureConnectionFactory)
    {
        return services.AddSingleton<IRabbitMqConnectionManager>((provider) =>
        {
            var rabbitMqConnectionManager = new RabbitMqConnectionManager();
            var configureConnection = configureConnectionFactory(provider);
            rabbitMqConnectionManager.ConfigureConnection(connectionFactory, configureConnection);
            return rabbitMqConnectionManager;
        });
    }
}