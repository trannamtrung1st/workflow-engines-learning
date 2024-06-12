namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IOutputEventPublisher
{
    void Publish(string @event);
}
