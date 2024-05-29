using System.Collections.Concurrent;
using System.Collections.Immutable;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    protected readonly IBlockExecutionControl _control;
    protected readonly ConcurrentDictionary<string, BlockBinding> _blockBindings;

    public BlockFramework(IBlockExecutionControl control)
    {
        _control = control;
        _blockBindings = new();
        _outputEvents = new();
    }

    private readonly HashSet<string> _outputEvents;
    public IImmutableSet<string> OutputEvents => _outputEvents.ToImmutableHashSet();

    public Task DelayAsync(int ms) => Task.Delay(ms);
    public void Delay(int ms) => Task.Delay(ms).Wait();

    public IBlockBinding Get(string name, bool isInternal = false)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, control: _control, isInternal: isInternal));

    public double GetDouble(string name, bool isInternal = false)
    {
        var binding = Get(name, isInternal);
        return binding.GetDouble();
    }

    public int GetInt(string name, bool isInternal = false)
    {
        var binding = Get(name, isInternal);
        return binding.GetInt();
    }

    public Task Set(string name, object value, bool isInternal = false)
    {
        var binding = Get(name, isInternal);
        return binding.Set(value);
    }

    public Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }
}
