using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockExecutionControl
{
    event EventHandler Running;
    event EventHandler<Exception> Failed;
    event EventHandler<BlockExecutionResult> Completed;

    FunctionBlockInstance Block { get; }
    string CurrentState { get; }
    bool IsIdle { get; }
    Exception Exception { get; }
    EBlockExecutionStatus Status { get; }

    ValueObject GetValueObject(string key, EBindingType type);
    ValueObject GetInOut(string key);
    ValueObject GetInput(string key);
    ValueObject GetOutput(string key);
    ValueObject GetInternalData(string key);
    Task<BlockExecutionResult> Execute(string triggerEvent,
        IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken);
    void WaitForIdle(CancellationToken cancellationToken);
    Task MutexAccess(Func<Task> task, CancellationToken cancellationToken);
}