namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IFunctionBlockWorker
{
    void StartWorkers(CancellationToken cancellationToken);
}