using System.Collections.Concurrent;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class MessageQueue : IMessageQueue
{
    private const int MaxMessagesCount = 10000;
    private readonly ConcurrentDictionary<string, TopicQueue> _queueMap;

    public MessageQueue()
    {
        _queueMap = new();
    }

    public T Consume<T>(string topic)
    {
        var topicQueue = _queueMap.GetOrAdd(topic, (key) => new TopicQueue(key));

        while (true)
        {
            if (topicQueue.Queue.TryDequeue(out var message))
                return (T)message;
            else
            {
                topicQueue.ReadyEvent.Reset();
                topicQueue.ReadyEvent.Wait();
            }
        }
    }

    public Task Publish<T>(string topic, T message)
    {
        var topicQueue = _queueMap.GetOrAdd(topic, (key) => new TopicQueue(key));
        if (topicQueue.Queue.Count < MaxMessagesCount)
        {
            topicQueue.Queue.Enqueue(message);
            topicQueue.ReadyEvent.Set();
        }
        return Task.CompletedTask;
    }

    class TopicQueue
    {
        public TopicQueue(string topic)
        {
            Topic = topic;
            Queue = new();
            ReadyEvent = new();
        }

        public string Topic { get; }
        public ConcurrentQueue<object> Queue { get; }
        public ManualResetEventSlim ReadyEvent { get; }
    }
}