using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    protected readonly IBasicEC _control;
    protected readonly ConcurrentDictionary<string, BlockBinding> _blockBindings;

    public BlockFramework(IBasicEC control)
    {
        _control = control;
        _blockBindings = new();
        _outputEvents = new();
    }

    private readonly HashSet<string> _outputEvents;
    public IEnumerable<string> OutputEvents => _outputEvents;

    public Task DelayAsync(int ms) => Task.Delay(ms);
    public void Delay(int ms) => Task.Delay(ms).Wait();

    public IInputBinding In(string name)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, valueObject: _control.GetInput(name)));

    public IOutputBinding Out(string name)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, valueObject: _control.GetOutput(name)));

    public IReadWriteBinding InOut(string name)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, valueObject: _control.GetInOut(name)));

    public IReadWriteBinding Internal(string name)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, valueObject: _control.GetInternalData(name)));

    public Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }
}
