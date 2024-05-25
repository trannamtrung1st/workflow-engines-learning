using System.Collections.Immutable;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkInstance : IBlockFrameworkInstance
{
    private readonly BlockFramework _blockFramework;

    public BlockFrameworkInstance(BlockFramework blockFramework)
    {
        _blockFramework = blockFramework;
        _outputEvents = new HashSet<string>();
    }

    private readonly HashSet<string> _outputEvents;
    public ImmutableHashSet<string> OutputEvents => _outputEvents.ToImmutableHashSet();

    public IBlockBinding Get(string name) => _blockFramework.Get(name);

    public Task Set(string name, object value) => _blockFramework.Set(name, value);

    public Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }

    public double GetDouble(string name)
    {
        var binding = Get(name);
        var value = binding.Value?.ToString();
        if (value == null) throw new ArgumentNullException(name);
        return double.Parse(value);
    }
}
