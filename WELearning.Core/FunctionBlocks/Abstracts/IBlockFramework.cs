using System.Collections.Immutable;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFramework
{
    IBlockBinding Get(string name, bool isInternal = false);
    double GetDouble(string name, bool isInternal = false);
    Task Set(string name, object value, bool isInternal = false);
    Task Publish(string eventName);
    IImmutableSet<string> OutputEvents { get; }
}
