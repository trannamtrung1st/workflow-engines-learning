namespace WELearning.DynamicCodeExecution.Models;

public readonly struct RunTokens
{
    public RunTokens(CancellationToken timeout, CancellationToken termination) : this()
    {
        Timeout = timeout;
        Termination = termination;
        Combined = CancellationTokenSource.CreateLinkedTokenSource(timeout, termination).Token;
    }

    public CancellationToken Timeout { get; }
    public CancellationToken Termination { get; }
    public CancellationToken Combined { get; }
}