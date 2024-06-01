using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IExecutionControl
{
    event EventHandler Running;
    event EventHandler<Exception> Failed;
    event EventHandler Completed;

    BlockInstance Block { get; }
    bool IsIdle { get; }
    Exception Exception { get; }
    EBlockExecutionStatus Status { get; }
    BlockExecutionResult Result { get; }

    ValueObject GetValueObject(string key, EVariableType type);
    ValueObject GetInOut(string key);
    ValueObject GetInput(string key);
    ValueObject GetOutput(string key);
    ValueObject GetInternalData(string key);
    Task Execute(string triggerEvent, IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken);
    void WaitForIdle(CancellationToken cancellationToken);
    Task MutexAccess(Func<Task> task, CancellationToken cancellationToken);
}
