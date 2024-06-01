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

    Variable GetVariable(string key, EVariableType type);
    IValueObject GetValueObject(string key, EVariableType type);
    IValueObject GetInOut(string key);
    IValueObject GetInput(string key);
    IValueObject GetOutput(string key);
    IValueObject GetInternalData(string key);
    void SetValueObject(string name, EVariableType type, IValueObject valueObject);
    Task Execute(string triggerEvent, IEnumerable<VariableBinding> bindings, Guid? optimizationScopeId, CancellationToken cancellationToken);
    void WaitForIdle(CancellationToken cancellationToken);
    Task MutexAccess(Func<Task> task, CancellationToken cancellationToken);
}
