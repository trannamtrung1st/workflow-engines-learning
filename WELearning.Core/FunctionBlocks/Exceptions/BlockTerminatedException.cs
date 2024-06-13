namespace WELearning.Core.FunctionBlocks.Exceptions;

public sealed class BlockTerminatedException : Exception
{
    public BlockTerminatedException(bool graceful, string message) : base(message)
    {
        Graceful = graceful;
    }

    public bool Graceful { get; }
}