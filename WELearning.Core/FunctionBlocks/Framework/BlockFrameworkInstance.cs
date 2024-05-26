using System.Collections.Immutable;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkInstance<TFramework, TFrameworkInstance> : IBlockFrameworkInstance
    where TFramework : BlockFramework<TFrameworkInstance>
{
    protected readonly TFramework _blockFramework;

    public BlockFrameworkInstance(TFramework blockFramework)
    {
        _blockFramework = blockFramework;
        _outputEvents = new HashSet<string>();
    }

    private readonly HashSet<string> _outputEvents;
    public IImmutableSet<string> OutputEvents => _outputEvents.ToImmutableHashSet();

    public IBlockBinding Get(string name) => _blockFramework.Get(name);

    public double GetDouble(string name) => _blockFramework.GetDouble(name);

    public Task Set(string name, object value) => _blockFramework.Set(name, value);

    public Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }
}
