namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFramework
{
    Task DelayAsync(int ms);
    void Delay(int ms);
    IReadBinding In(string name);
    IWriteBinding Out(string name);
    IReadWriteBinding Internal(string name);
    Task Publish(string eventName);
    IEnumerable<string> OutputEvents { get; }
}
