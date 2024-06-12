namespace WELearning.Shared.Concurrency;

public sealed class SimpleScope : IDisposable
{
    private readonly Action _onDispose;

    public SimpleScope()
    {
    }

    public SimpleScope(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose() => _onDispose?.Invoke();
}