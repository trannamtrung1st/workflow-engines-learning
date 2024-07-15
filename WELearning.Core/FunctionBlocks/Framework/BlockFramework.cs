using System.Collections.Concurrent;
using System.Dynamic;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    protected readonly IExecutionControl _control;
    protected readonly ConcurrentDictionary<string, IReadBinding> _inputBindings;
    protected readonly ConcurrentDictionary<string, IWriteBinding> _outputBindings;
    protected readonly ConcurrentDictionary<string, IReadWriteBinding> _inOutBindings;
    protected readonly ConcurrentDictionary<string, IReadWriteBinding> _internalBindings;

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
    public virtual IEnumerable<string> OutputEvents => _outputEvents;

    public virtual IReadOnlyDictionary<string, IReadBinding> InputBindings => _inputBindings;
    public virtual IReadOnlyDictionary<string, IWriteBinding> OutputBindings => _outputBindings;
    public virtual IReadOnlyDictionary<string, IReadWriteBinding> InOutBindings => _inOutBindings;
    public virtual IReadOnlyDictionary<string, IReadWriteBinding> InternalBindings => _internalBindings;

    public virtual object GetBindingFor(IValueObject valueObject)
    {
        var variable = valueObject.Variable;
        var name = variable.Name;
        switch (variable.VariableType)
        {
            case EVariableType.Input: return In(name);
            case EVariableType.Output: return Out(name);
            case EVariableType.InOut:
                {
                    var binding = InOut(name);
                    _inputBindings[name] = binding;
                    _outputBindings[name] = binding;
                    return binding;
                }
            case EVariableType.Internal: return Internal(name);
            default: throw new NotSupportedException($"Variable type {variable.VariableType} not supported!");
        }
    }

    public virtual void HandleDynamicResult(dynamic result)
    {
        if (result is not ExpandoObject expObj) return;
        foreach (var kvp in expObj)
        {
            var variable = _control.GetVariable(kvp.Key, Constants.EVariableType.Output)
                ?? _control.GetVariable(kvp.Key, Constants.EVariableType.InOut)
                ?? _control.GetVariable(kvp.Key, Constants.EVariableType.Internal);
            IWriteBinding writeBinding = null;
            switch (variable.VariableType)
            {
                case EVariableType.Output: writeBinding = Out(kvp.Key); break;
                case EVariableType.InOut: writeBinding = InOut(kvp.Key); break;
                case EVariableType.Internal: writeBinding = Internal(kvp.Key); break;
            }
            writeBinding?.Write(kvp.Value);
        }
    }

    protected virtual IReadBinding In(string name)
        => _inputBindings.GetOrAdd(name, (key) => new ReadBinding(key, valueObject: _control.GetInput(name)));

    protected virtual IWriteBinding Out(string name)
        => _outputBindings.GetOrAdd(name, (key) => new WriteBinding(key, valueObject: _control.GetOutput(name)));

    protected virtual IReadWriteBinding InOut(string name)
        => _inOutBindings.GetOrAdd(name, (key) => new ReadWriteBinding(key, valueObject: _control.GetInOut(name)));

    protected virtual IReadWriteBinding Internal(string name)
        => _internalBindings.GetOrAdd(name, (key) => new InternalBinding(key, valueObject: _control.GetInternalData(name)));

    public IOutputEventPublisher CreateEventPublisher(HashSet<string> outputEvents) => new SimpleEventPublisher(outputEvents);

    class SimpleEventPublisher : IOutputEventPublisher
    {
        private readonly HashSet<string> _outputEvents;
        public SimpleEventPublisher(HashSet<string> outputEvents)
        {
            _outputEvents = outputEvents;
        }

        public void Publish(string @event) => _outputEvents.Add(@event);
    }
}
