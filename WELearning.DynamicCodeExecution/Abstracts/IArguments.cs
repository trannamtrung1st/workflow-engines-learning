namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IArguments
{
    IReadOnlyDictionary<string, object> GetArguments();
}