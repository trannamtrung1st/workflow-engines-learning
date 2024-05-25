using System.Collections.Immutable;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFrameworkInstance
{
    IBlockBinding Get(string name);
    double GetDouble(string name);
    Task Set(string name, object value);
    Task Publish(string eventName);
    ImmutableHashSet<string> OutputEvents { get; }
}
