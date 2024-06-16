using RabbitMQ.Client;

namespace WELearning.Samples.Shared.RabbitMq.Abstracts;

public interface IRabbitMqConnectionManager
{
    IConnection Connection { get; }
    IModel GetChannel(string channelId);

    void ConfigureConnection(ConnectionFactory connectionFactory, Action<IConnection> configure);
    void ConfigureChannel(string channelId, Action<IModel> configure);
    void Connect();
    void Close();
    void Connect(string channelId);
    void Close(string channelId);
}