using WELearning.Samples.DeviceService.Models;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IFunctionBlockService
{
    Task HandleAttributeChanged(AttributeChangedEvent @event, CancellationToken cancellationToken);
}
