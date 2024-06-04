using System.Runtime.Serialization;

namespace WELearning.DynamicCodeExecution.Exceptions;

public class TerminationException : Exception
{
    public TerminationException()
    {
    }

    public TerminationException(string message) : base(message)
    {
    }

    public TerminationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected TerminationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}