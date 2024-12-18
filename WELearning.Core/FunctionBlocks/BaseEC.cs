using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public abstract class BaseEC<TDefinition> : IExecutionControl, IDisposable where TDefinition : BaseBlockDef
{
    private readonly object _handleFailedLock = new();
    private readonly ManualResetEventSlim _idleWait;
    private readonly List<Variable> _variables;
    protected readonly ConcurrentDictionary<Variable, IValueObject> _valueMap;
    protected readonly SemaphoreSlim _mutexLock;
    protected readonly IFunctionRunner _functionRunner;
    protected readonly IBlockFrameworkFactory _blockFrameworkFactory;

    public BaseEC(
        BlockInstance block,
        TDefinition definition,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        bool printErrorLocation = false)
    {
        _functionRunner = functionRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _valueMap = new();
        _idleWait = new(initialState: true);
        _mutexLock = new(1, 1);
        _variables = new(definition.Variables);
        Block = block;
        Definition = definition;
        PrintError = printErrorLocation;
    }

    protected virtual bool PrintError { get; }
    public RunBlockRequest CurrentRunRequest { get; protected set; }
    public BlockInstance Block { get; }
    public TDefinition Definition { get; }
    public virtual Exception Exception { get; protected set; }
    public virtual bool IsIdle => _idleWait.IsSet;
    public virtual EBlockExecutionStatus Status { get; protected set; }
    public virtual BlockExecutionResult Result { get; protected set; }
    public virtual BlockActivity LastActivity { get; protected set; }

    public event EventHandler Running;
    public event EventHandler<Exception> Failed;
    public event EventHandler Completed;
    public event EventHandler Idle;

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
        valueObject.TryCastAndSet(value);
    }

    public virtual Variable GetVariable(string key, EVariableType type)
    {
        var isInOrOut = type == EVariableType.Input || type == EVariableType.Output || type == EVariableType.InOut;
        var variable = GetVariables().FirstOrDefault(v => v.Name == key
            && (
                v.VariableType == type
                || (v.VariableType == EVariableType.InOut && isInOrOut)
            ));
        return variable;
    }

    public virtual IEnumerable<Variable> GetVariables() => _variables;

    public virtual void AddDynamicVariable(Variable variable)
    {
        lock (_variables)
        {
            _variables.Add(variable);
        }
    }

    protected virtual void SetIdle()
    {
        Idle?.Invoke(this, EventArgs.Empty);
        _idleWait.Set();
    }

    protected virtual Variable ValidateVariable(string name, EVariableType type)
        => GetVariable(name, type) ?? throw new KeyNotFoundException($"Variable {name} not found!");

    protected virtual void PrepareRunningStatus(RunBlockRequest request)
    {
        CurrentRunRequest = request;
        Exception = null; Result = null;
        Status = EBlockExecutionStatus.Running;
        LastActivity = new BlockActivity(this, runRequest: request);
    }

    protected virtual void HandleRunning(RunBlockRequest request)
    {
        PrepareRunningStatus(request);
        PublishRunning();
        PrepareStates(request.Bindings);
    }

    protected virtual void HandleException(Exception ex, IExecutionControl from = null)
    {
        lock (_handleFailedLock)
        {
            if (Status != EBlockExecutionStatus.Running) return;

            if (ex is FunctionRuntimeException runtimeEx && runtimeEx.IsGracefulTerminated())
                Status = EBlockExecutionStatus.Completed;

            if (Status == EBlockExecutionStatus.Running)
                Status = EBlockExecutionStatus.Failed;
        }

        if (Status == EBlockExecutionStatus.Completed)
            HandleCompleted(force: true);
        else
        {
            PrepareFailedStatus(ex, from);
            PublishFailed(ex);
        }

        if (!CurrentRunRequest.Tokens.ExceptionCts.IsCancellationRequested)
            CurrentRunRequest.Tokens.ExceptionCts.Cancel();
    }

    protected virtual void HandleCompleted(bool force = false)
    {
        if (force || Status == EBlockExecutionStatus.Running)
        {
            PrepareCompletedStatus();
            PublishCompleted();
        }
    }

    protected void PublishFailed(Exception ex) => Failed?.Invoke(this, ex);
    protected void PublishCompleted() => Completed?.Invoke(this, EventArgs.Empty);
    protected void PublishRunning() => Running?.Invoke(this, EventArgs.Empty);

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

    public IReadOnlyDictionary<string, object> GetReservedInputs() => CurrentRunRequest?.ReservedInputs;

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

    public abstract Task Execute(RunBlockRequest request, string optimizationScopeId);
    protected abstract void RefreshOutputs();

    public virtual void LogFailure(Exception ex, ILogger logger = null)
    {
        string messageFormat =
@"=== {0} ===
+ Block: {1}
+ Function: {2}
+ Description: {3}
+ Location (line, column, index): ({4}, {5}, {6})
+ Source: {7}";
        if (ex is FunctionCompilationError error)
        {
            var compErr = error.Error;
            var message = string.Format(format: messageFormat,
                "Compilation error",
                (this as ICompositeEC)?.ExceptionFrom.Block.Id ?? Block.Id,
                (this as IBasicEC)?.RunningFunction?.Id,
                compErr.Description,
                compErr.LineNumber,
                compErr.Column,
                compErr.StartIndex,
                compErr.Source);

            if (logger != null)
                logger.LogError(message);
            else Console.Error.WriteLine(message);

            if (PrintError)
                error.PrintErrorLocation(logger: logger);
        }
        else if (ex is FunctionRuntimeException runtimeEx)
        {
            var exception = runtimeEx.Exception;
            var message = string.Format(format: messageFormat,
                $"Runtime exception ({exception.Source})",
                (this as ICompositeEC)?.ExceptionFrom.Block.Id ?? Block.Id,
                (this as IBasicEC)?.RunningFunction?.Id,
                exception.Description,
                exception.LineNumber,
                exception.Column,
                exception.StartIndex,
                exception.Source);

            if (logger != null)
                logger.LogError(message);
            else Console.Error.WriteLine(message);

            if (PrintError)
                runtimeEx.PrintErrorLocation(logger: logger);
        }
        else
        {
            var message = string.Format(format: messageFormat,
                $"System exception",
                (this as ICompositeEC)?.ExceptionFrom.Block.Id ?? Block.Id,
                (this as IBasicEC)?.RunningFunction?.Id,
                ex.Message, -1, -1, -1, ex.Source);

            if (logger != null)
                logger.LogError(message);
            else Console.Error.WriteLine(message);
        }
    }

    public virtual void LogBlockActivity(ILogger logger = null)
    {
        if (LastActivity == null)
            return;
        string messageFormat =
@"
=== {0} ===
+ Run ID: {1}
+ Time (UTC): {2}
+ Status: {3}
+ Run time (ms): {4}
+ Exception from block: {5}
";
        var message = string.Format(format: messageFormat,
            $"{(LastActivity.Control is ICompositeEC ? "CFB" : "BFB")}: {LastActivity.Control.Block.Id}",
            LastActivity.RunRequest.RunId,
            LastActivity.TimeUtc,
            LastActivity.Status,
            LastActivity.RunTime?.TotalMilliseconds.ToString() ?? "N/A",
            LastActivity.ExceptionFrom?.Block.Id ?? "N/A");

        if (logger != null)
            logger.LogTrace(message);
        else Console.WriteLine(message);

        if (LastActivity.Status == EBlockExecutionStatus.Failed)
            LogFailure(LastActivity.Exception, logger);
    }

    public bool RegisterTempIdleCallback(Func<Task> callback)
    {
        lock (_idleWait)
        {
            var registered = !IsIdle;
            if (registered)
            {
                void Handle(object o, EventArgs e)
                {
                    Idle -= Handle;
                    _ = callback();
                }
                Idle += Handle;
            }
            return registered;
        }
    }
}
