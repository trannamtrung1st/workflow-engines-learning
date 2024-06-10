namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IMessageQueue
{
    Task Publish<T>(string topic, T message);
    T Consume<T>(string topic);
}