namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IOptimizationScope : IDisposable
{
    string Id { get; }
    bool TryDispose();
}
