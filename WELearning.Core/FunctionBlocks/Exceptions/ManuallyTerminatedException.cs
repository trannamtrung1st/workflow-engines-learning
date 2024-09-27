namespace WELearning.Core.FunctionBlocks.Exceptions;

public sealed class ManuallyTerminatedException : BlockException
{
    public ManuallyTerminatedException(bool graceful, string message) : base(message)
    {
        Graceful = graceful;
    }

    public bool Graceful { get; }
}
