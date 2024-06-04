namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFramework
{
    Task DelayAsync(int ms);
    void Delay(int ms);
    IReadBinding In(string name);
    IWriteBinding Out(string name);
    IReadWriteBinding InOut(string name);
    IReadWriteBinding Internal(string name);
    Task Publish(string eventName);
    IEnumerable<string> OutputEvents { get; }
    Task HandleDynamicResult(dynamic result);
    void Log(params object[] data);
    void LogError(params object[] data);
    void LogWarning(params object[] data);
}
