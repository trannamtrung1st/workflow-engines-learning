using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    protected readonly IExecutionControl _control;
    protected readonly ConcurrentDictionary<string, InputBinding> _inputBindings;
    protected readonly ConcurrentDictionary<string, OutputBinding> _outputBindings;
    protected readonly ConcurrentDictionary<string, InOutBinding> _inOutBindings;
    protected readonly ConcurrentDictionary<string, InternalBinding> _internalBindings;

    public BlockFramework(IExecutionControl control)
    {
        _control = control;
        _inputBindings = new();
        _outputBindings = new();
        _inOutBindings = new();
        _internalBindings = new();
        _outputEvents = new();
    }

    private readonly HashSet<string> _outputEvents;
    public IEnumerable<string> OutputEvents => _outputEvents;

    public Task DelayAsync(int ms) => Task.Delay(ms);
    public void Delay(int ms) => Task.Delay(ms).Wait();

    public IReadBinding In(string name)
        => _inputBindings.GetOrAdd(name, (key) => new InputBinding(key, valueObject: _control.GetInput(name)));

    public IWriteBinding Out(string name)
        => _outputBindings.GetOrAdd(name, (key) => new OutputBinding(key, valueObject: _control.GetOutput(name)));

    public IReadWriteBinding InOut(string name)
        => _inOutBindings.GetOrAdd(name, (key) => new InOutBinding(key, valueObject: _control.GetInOut(name)));

    public IReadWriteBinding Internal(string name)
        => _internalBindings.GetOrAdd(name, (key) => new InternalBinding(key, valueObject: _control.GetInternalData(name)));

    public Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }
}
