namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class CompilationError : Exception
{
    public abstract string Description { get; }
    public abstract string RawMessage { get; }
    public abstract int LineNumber { get; }
    public abstract int Column { get; }
    public abstract int Index { get; }
    public abstract bool IsSystemError { get; }
}