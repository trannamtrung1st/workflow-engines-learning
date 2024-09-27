namespace WELearning.Core.FunctionBlocks.Exceptions;

public abstract class BlockException : Exception
{
    protected BlockException()
    {
    }

    protected BlockException(string message) : base(message)
    {
    }

    protected BlockException(string message, Exception innerException) : base(message, innerException)
    {
    }
}