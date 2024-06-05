using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IExecutionControl : IDisposable
{
    event EventHandler Running;
    event EventHandler<Exception> Failed;
    event EventHandler Completed;

    RunBlockRequest CurrentRunRequest { get; }
    BlockInstance Block { get; }
    bool IsIdle { get; }
    Exception Exception { get; }
    EBlockExecutionStatus Status { get; }
    BlockExecutionResult Result { get; }
    BlockActivity LastActivity { get; }

    Variable GetVariable(string key, EVariableType type);
    IEnumerable<Variable> GetVariables();
    IValueObject GetInput(string key);
    IValueObject GetOutput(string key);
    IValueObject GetInOut(string key);
    IValueObject GetInternalData(string key);
    void SetReference(string name, EVariableType type, IValueObject reference);
    void SetValue(string name, EVariableType type, object value);
    Task Execute(RunBlockRequest request, Guid? optimizationScopeId);
    void WaitForIdle(CancellationToken cancellationToken);
    Task MutexAccess(Func<Task> task, CancellationToken cancellationToken);
}
