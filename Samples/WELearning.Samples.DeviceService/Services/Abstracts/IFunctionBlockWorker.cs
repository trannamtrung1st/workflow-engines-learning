namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IFunctionBlockWorker
{
    void StartWorker(CancellationToken cancellationToken);
    void StartDynamicScalingWorker();
    void StopDynamicScalingWorker();
}