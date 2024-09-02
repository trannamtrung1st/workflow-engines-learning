namespace WELearning.DynamicCodeExecution.Models;

public class RunTokens : IDisposable
{
    private readonly CancellationTokenSource _timeoutCts;
    private readonly CancellationTokenSource _combinedCts;
    private readonly CancellationTokenSource _exceptionCts;
    public RunTokens(TimeSpan timeout, CancellationToken termination)
    {
        _timeoutCts = new CancellationTokenSource(timeout);
        _exceptionCts = new CancellationTokenSource();
        _combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            _timeoutCts.Token, termination, _exceptionCts.Token);
        Timeout = _timeoutCts.Token;
        Termination = termination;
        Exception = _exceptionCts.Token;
        Combined = _combinedCts.Token;
        ExceptionCts = _exceptionCts;
    }

    public CancellationToken Timeout { get; }
    public CancellationToken Termination { get; }
    public CancellationToken Exception { get; }
    public CancellationToken Combined { get; }
    public CancellationTokenSource ExceptionCts { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        using var _ = _timeoutCts;
        using var _1 = _exceptionCts;
        using var _2 = _combinedCts;
    }
}
