namespace WELearning.DynamicCodeExecution.Exceptions;

public class TerminatedException : Exception
{
    public TerminatedException()
    {
    }

    public TerminatedException(string message) : base(message)
    {
    }

    public TerminatedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}