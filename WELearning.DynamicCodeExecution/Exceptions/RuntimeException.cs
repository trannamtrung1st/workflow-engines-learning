namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class RuntimeException : EngineException
{
    public const string SourceUser = "User";
    public const string SourceSystem = "System";
}