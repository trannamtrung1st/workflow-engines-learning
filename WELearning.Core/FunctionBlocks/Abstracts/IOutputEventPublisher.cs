namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IOutputEventPublisher
{
    void Publish(string @event);
}
