using System.Collections.Concurrent;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class MessageQueue : IMessageQueue
{
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
            topicQueue.ReadyEvent.Wait();
            lock (topicQueue)
            {
                if (topicQueue.Queue.TryDequeue(out var message))
                {
                    if (topicQueue.Queue.IsEmpty)
                        topicQueue.ReadyEvent.Reset();
                    return (T)message;
                }
            }
        }

    }

    public Task Publish<T>(string topic, T message)
    {
        var topicQueue = _queueMap.GetOrAdd(topic, (key) => new TopicQueue(key));
        lock (topicQueue)
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