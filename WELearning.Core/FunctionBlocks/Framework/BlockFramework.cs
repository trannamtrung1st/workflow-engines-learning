using System.Collections.Concurrent;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    private readonly ILogger<BlockFramework> _logger;
    protected readonly IExecutionControl _control;
    protected readonly ConcurrentDictionary<string, InputBinding> _inputBindings;
    protected readonly ConcurrentDictionary<string, OutputBinding> _outputBindings;
    protected readonly ConcurrentDictionary<string, InOutBinding> _inOutBindings;
    protected readonly ConcurrentDictionary<string, InternalBinding> _internalBindings;

    public BlockFramework(IExecutionControl control, ILogger<BlockFramework> logger)
    {
        _logger = logger;
        _control = control;
        _inputBindings = new();
        _outputBindings = new();
        _inOutBindings = new();
        _internalBindings = new();
        _outputEvents = new();
    }

    private readonly HashSet<string> _outputEvents;
    public virtual IEnumerable<string> OutputEvents => _outputEvents;

    public virtual Task DelayAsync(int ms) => Task.Delay(ms);

    public virtual void Delay(int ms) => DelayAsync(ms).Wait();

    public virtual IReadBinding In(string name)
        => _inputBindings.GetOrAdd(name, (key) => new InputBinding(key, valueObject: _control.GetInput(name)));

    public virtual IWriteBinding Out(string name)
        => _outputBindings.GetOrAdd(name, (key) => new OutputBinding(key, valueObject: _control.GetOutput(name)));

    public virtual IReadWriteBinding InOut(string name)
        => _inOutBindings.GetOrAdd(name, (key) => new InOutBinding(key, valueObject: _control.GetInOut(name)));

    public virtual IReadWriteBinding Internal(string name)
        => _internalBindings.GetOrAdd(name, (key) => new InternalBinding(key, valueObject: _control.GetInternalData(name)));

    public virtual Task Publish(string eventName)
    {
        _outputEvents.Add(eventName);
        return Task.CompletedTask;
    }

    public virtual Task HandleDynamicResult(dynamic result)
    {
        if (result is not ExpandoObject expObj) return Task.CompletedTask;
        foreach (var kvp in expObj)
        {
            switch (kvp.Key)
            {
                case BuiltInVariables.EventsOutputVariable:
                    {
                        var events = kvp.Value as IEnumerable<object>;
                        if (events?.Any() == true)
                            foreach (var ev in events)
                                if (ev is string evStr)
                                    _outputEvents.Add(evStr);
                        break;
                    }
                default:
                    {
                        var variable = _control.GetVariable(kvp.Key, Constants.EVariableType.Output)
                            ?? _control.GetVariable(kvp.Key, Constants.EVariableType.InOut)
                            ?? _control.GetVariable(kvp.Key, Constants.EVariableType.Internal);
                        IWriteBinding writeBinding = null;
                        switch (variable.VariableType)
                        {
                            case Constants.EVariableType.Output: writeBinding = Out(kvp.Key); break;
                            case Constants.EVariableType.InOut: writeBinding = InOut(kvp.Key); break;
                            case Constants.EVariableType.Internal: writeBinding = Internal(kvp.Key); break;
                        }
                        writeBinding?.Write(kvp.Value);
                        break;
                    }
            }
        }
        return Task.CompletedTask;
    }

    public virtual void Log(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogInformation(message);
    }

    public virtual void LogError(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogError(message);
    }

    public virtual void LogWarning(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogWarning(message);
    }

    public static string GetLogMessage(object[] data) => string.Join(' ', data);
}
