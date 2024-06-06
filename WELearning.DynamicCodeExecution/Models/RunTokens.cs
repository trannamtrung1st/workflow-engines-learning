namespace WELearning.DynamicCodeExecution.Models;

public class RunTokens : IDisposable
{
    private readonly CancellationTokenSource _timeoutCts;
    private readonly CancellationTokenSource _combinedCts;
    public RunTokens(TimeSpan timeout, CancellationToken termination)
    {
        _timeoutCts = new CancellationTokenSource(timeout);
        _combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_timeoutCts.Token, termination);
        Timeout = _timeoutCts.Token;
        Termination = termination;
        Combined = _combinedCts.Token;
    }

    public CancellationToken Timeout { get; }
    public CancellationToken Termination { get; }
    public CancellationToken Combined { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _timeoutCts.Dispose();
        _combinedCts.Dispose();
    }
}