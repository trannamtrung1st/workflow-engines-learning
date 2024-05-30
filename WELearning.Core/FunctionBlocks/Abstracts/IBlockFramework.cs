using System.Collections.Immutable;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFramework
{
    Task DelayAsync(int ms);
    void Delay(int ms);
    IInputBinding In(string name);
    IOutputBinding Out(string name);
    IReadWriteBinding Internal(string name);
    Task Publish(string eventName);
    IImmutableSet<string> OutputEvents { get; }
}
