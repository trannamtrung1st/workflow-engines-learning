using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public abstract class BaseEC<TFramework, TDefinition> : IExecutionControl, IDisposable
    where TFramework : IBlockFramework
    where TDefinition : BaseBlockDef
{
    private readonly object _handleFailedLock = new();
    protected readonly ConcurrentDictionary<Variable, IValueObject> _valueMap;
    protected readonly SemaphoreSlim _mutexLock;
    protected readonly ManualResetEventSlim _idleWait;
    protected readonly IFunctionRunner<TFramework> _functionRunner;
    protected readonly IBlockFrameworkFactory<TFramework> _blockFrameworkFactory;
    public BaseEC(
        BlockInstance block,
        TDefinition definition,
        IFunctionRunner<TFramework> functionRunner,
        IBlockFrameworkFactory<TFramework> blockFrameworkFactory)
    {
        _functionRunner = functionRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _valueMap = new();
        _idleWait = new(initialState: true);
        _mutexLock = new(1);
        Block = block;
        Definition = definition;
    }

    public Guid? CurrentRunId { get; protected set; }
    public BlockInstance Block { get; }
    public TDefinition Definition { get; }
    public virtual Exception Exception { get; protected set; }
    public virtual bool IsIdle => _idleWait.IsSet;
    public virtual EBlockExecutionStatus Status { get; protected set; }
    public virtual BlockExecutionResult Result { get; protected set; }
    public virtual BlockActivity LastActivity { get; protected set; }
    public virtual CancellationToken RunCancellationToken { get; protected set; }

    public abstract event EventHandler Running;
    public abstract event EventHandler<Exception> Failed;
    public abstract event EventHandler Completed;

    public virtual IValueObject GetInOut(string key) => GetValueObject(key, EVariableType.InOut);
    public virtual IValueObject GetInput(string key) => GetValueObject(key, EVariableType.Input);
    public virtual IValueObject GetOutput(string key) => GetValueObject(key, EVariableType.Output);
    public virtual IValueObject GetInternalData(string key) => GetValueObject(key, EVariableType.Internal);
    public virtual IValueObject GetValueObject(string name, EVariableType type)
    {
        var variable = ValidateVariable(name, type);
        return _valueMap.GetOrAdd(variable, (variable) => new RawValueObject(variable));
    }

    public virtual void SetReference(string name, EVariableType type, IValueObject valueObject)
    {
        var variable = ValidateVariable(name, type);
        _valueMap[variable] = valueObject;
    }

    public virtual void SetValue(string name, EVariableType type, object value)
    {
        var valueObject = GetValueObject(name, type);
        valueObject.Value = value;
    }

    public virtual Variable GetVariable(string key, EVariableType type)
    {
        var isInOrOut = type == EVariableType.Input || type == EVariableType.Output || type == EVariableType.InOut;
        var variable = Definition.Variables.FirstOrDefault(v => v.Name == key
            && (
                v.VariableType == type
                || (v.VariableType == EVariableType.InOut && isInOrOut)
            ));
        return variable;
    }

    public virtual IEnumerable<Variable> GetVariables() => Definition.Variables;

    protected virtual Variable ValidateVariable(string name, EVariableType type)
        => GetVariable(name, type) ?? throw new KeyNotFoundException(name);

    protected virtual void PrepareRunningStatus(RunBlockRequest runRequest, CancellationToken cancellationToken)
    {
        CurrentRunId = runRequest.RunId;
        RunCancellationToken = cancellationToken;
        Exception = null; Result = null;
        Status = EBlockExecutionStatus.Running;
        LastActivity = new BlockActivity(this, runRequest: runRequest);
    }

    protected virtual bool SetFailedStatus()
    {
        lock (_handleFailedLock)
        {
            if (Status == EBlockExecutionStatus.Failed) return false;
            Status = EBlockExecutionStatus.Failed;
        }
        return true;
    }

    protected virtual void PrepareFailedStatus(Exception ex, IExecutionControl from = null)
    {
        Exception = ex;
        LastActivity = new BlockActivity(this, runRequest: LastActivity.RunRequest);
    }

    protected virtual void PrepareCompletedStatus()
    {
        RefreshOutputs();
        Status = EBlockExecutionStatus.Completed;
        LastActivity = new BlockActivity(this, runRequest: LastActivity.RunRequest);
    }

    protected virtual void EnterOrThrow()
    {
        lock (_idleWait)
        {
            if (!IsIdle) throw new InvalidOperationException("Not ready for execute!");
            _idleWait.Reset();
        }
    }

    protected virtual string GetTriggerOrDefault(string triggerEvent)
        => triggerEvent ?? Definition.DefaultTriggerEvent;

    protected virtual void PrepareStates(IEnumerable<VariableBinding> bindings)
    {
        foreach (var binding in bindings.Where(b => b.Reference != null))
        {
            var variableType = binding.Type.ToVariableType();
            SetReference(binding.VariableName, variableType, binding.Reference);
        }

        foreach (var binding in bindings.Where(b => b.Reference == null))
        {
            var variableType = binding.Type.ToVariableType();
            SetValue(binding.VariableName, variableType, binding.Value);
        }
    }

    protected virtual void FlattenInputs(List<(string, object)> flatten)
    {
        var variables = Definition.Variables.Where(v => v.CanInput());
        foreach (var variable in variables)
        {
            if (_valueMap.TryGetValue(variable, out var valueObject))
                flatten.Add((variable.Name, valueObject.Value));
        }
    }

    protected virtual void FlattenOutputs(List<string> flatten)
    {
        var variables = Definition.Variables.Where(v => v.CanOutput());
        foreach (var variable in variables)
            flatten.Add(variable.Name);
    }

    public virtual void WaitForIdle(CancellationToken cancellationToken) => _idleWait.Wait(cancellationToken);

    public virtual async Task MutexAccess(Func<Task> Task, CancellationToken cancellationToken)
    {
        await _mutexLock.WaitAsync(cancellationToken);
        try { await Task(); }
        finally { _mutexLock.Release(); }
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _mutexLock.Dispose();
        _idleWait.Dispose();
    }

    public abstract Task Execute(RunBlockRequest request, Guid? optimizationScopeId, CancellationToken cancellationToken);
    protected abstract void RefreshOutputs();
}
