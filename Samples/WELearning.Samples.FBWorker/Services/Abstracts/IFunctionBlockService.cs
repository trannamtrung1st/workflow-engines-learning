using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.Services.Abstracts;

public interface IFunctionBlockService
{
    Task HandleAttributeChanged(AttributeChangedEvent @event, CancellationToken cancellationToken);
}
