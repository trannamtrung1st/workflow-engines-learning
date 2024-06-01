using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public abstract class BaseEC<TFramework, TDefinition> : IExecutionControl, IDisposable
    where TFramework : IBlockFramework
    where TDefinition : BaseBlockDef
{
    protected readonly ConcurrentDictionary<Variable, ValueObject> _valueMap;
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

    public BlockInstance Block { get; }
    public TDefinition Definition { get; }
    public virtual Exception Exception { get; protected set; }
    public virtual bool IsIdle => _idleWait.IsSet;
    public virtual EBlockExecutionStatus Status { get; protected set; }
    public abstract BlockExecutionResult Result { get; }

    public abstract event EventHandler Running;
    public abstract event EventHandler<Exception> Failed;
    public abstract event EventHandler Completed;

    public virtual ValueObject GetInOut(string key) => GetValueObject(key, EVariableType.InOut);
    public virtual ValueObject GetInput(string key) => GetValueObject(key, EVariableType.Input);
    public virtual ValueObject GetOutput(string key) => GetValueObject(key, EVariableType.Output);
    public virtual ValueObject GetInternalData(string key) => GetValueObject(key, EVariableType.Internal);
    public virtual ValueObject GetValueObject(string name, EVariableType type)
    {
        var variable = ValidateBinding(name, type);
        return _valueMap.GetOrAdd(variable, (variable) => new ValueObject(variable));
    }

    protected virtual Variable ValidateBinding(string name, EVariableType type)
    {
        var isInOrOut = type == EVariableType.Input || type == EVariableType.Output || type == EVariableType.InOut;
        var variable = Definition.Variables.FirstOrDefault(v => v.Name == name
            && (
                v.VariableType == type
                || (v.VariableType == EVariableType.InOut && isInOrOut)
            ));
        return variable ?? throw new KeyNotFoundException(name);
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

    public abstract Task Execute(string triggerEvent, IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken);
}
