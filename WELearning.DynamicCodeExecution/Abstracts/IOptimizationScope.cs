namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IOptimizationScope : IDisposable
{
    Guid Id { get; }
}