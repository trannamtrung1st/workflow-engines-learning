namespace WELearning.DynamicCodeExecution.Models;

public struct RunTokens
{
    public RunTokens(CancellationToken timeout, CancellationToken termination, CancellationToken combined) : this()
    {
        Timeout = timeout;
        Termination = termination;
        Combined = combined;
    }

    public CancellationToken Timeout { get; set; }
    public CancellationToken Termination { get; set; }
    public CancellationToken Combined { get; set; }
}