namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IOutputEventPublisher
{
    Task Publish(string @event);
}
