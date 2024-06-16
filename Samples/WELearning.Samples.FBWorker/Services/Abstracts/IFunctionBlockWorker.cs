namespace WELearning.Samples.FBWorker.Services.Abstracts;

public interface IFunctionBlockWorker
{
    void StartWorker(CancellationToken cancellationToken);
    void StartDynamicScalingWorker();
    void StopDynamicScalingWorker();
}