using System.Dynamic;
using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework(IExecutionControl control, ILogger logger) : IBlockFramework
{
    public static class ReservedInputs
    {
        public const string Console = "console";
    }

    public IExecutionControl Control { get; } = control;

    private IFrameworkConsole _console;
    public virtual IFrameworkConsole GetFrameworkConsole() => _console ??= new FrameworkConsole(logger);

    private IReadOnlyDictionary<string, object> _reservedInputs;
    public virtual IReadOnlyDictionary<string, object> GetReservedInputs() => _reservedInputs ??= new Dictionary<string, object>()
    {
        [ReservedInputs.Console] = GetFrameworkConsole()
    };

    public virtual object GetBindingFor(IValueObject valueObject)
    {
        var variable = valueObject.Variable;
        var name = variable.Name;
        switch (variable.VariableType)
        {
            case EVariableType.Input: return In(name);
            case EVariableType.Output: return Out(name);
            case EVariableType.InOut: return InOut(name);
            case EVariableType.Internal: return Internal(name);
            default: throw new NotSupportedException($"Variable type {variable.VariableType} not supported!");
        }
    }

    public virtual object GetBindingFor(string name, EVariableType variableType)
    {
        var valueObject = Control.GetValueObject(name, variableType);
        return GetBindingFor(valueObject);
    }

    public virtual void HandleDynamicResult(dynamic result, Function function)
    {
        if (result is not ExpandoObject expObj)
            return;
        foreach (var kvp in expObj)
            HandleResultKvp(kvp.Key, kvp.Value, function);
    }

    protected virtual void HandleResultKvp(string key, object value, Function function)
    {
        var variable = TryGetWritableVariable(key);
        IWriteBinding writeBinding = null;
        switch (variable.VariableType)
        {
            case EVariableType.Output:
                writeBinding = Out(key);
                break;
            case EVariableType.InOut:
                writeBinding = InOut(key);
                break;
            case EVariableType.Internal:
                writeBinding = Internal(key);
                break;
        }
        writeBinding?.Write(value);
    }

    protected virtual Variable TryGetWritableVariable(string key)
    {
        var variable = Control.GetVariable(key, Constants.EVariableType.Output)
            ?? Control.GetVariable(key, Constants.EVariableType.InOut)
            ?? Control.GetVariable(key, Constants.EVariableType.Internal);
        return variable;
    }

    protected virtual IReadBinding In(string name) => new ReadBinding(name, valueObject: Control.GetInput(name));

    protected virtual IWriteBinding Out(string name) => new WriteBinding(name, valueObject: Control.GetOutput(name));
    protected virtual IReadWriteBinding InOut(string name) => new ReadWriteBinding(name, valueObject: Control.GetInOut(name));

    protected virtual IReadWriteBinding Internal(string name) => new InternalBinding(name, valueObject: Control.GetInternalData(name));

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
