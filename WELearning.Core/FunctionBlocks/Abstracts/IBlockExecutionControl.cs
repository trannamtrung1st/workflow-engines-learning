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
    bool IsRunning { get; }
    Exception Exception { get; }
    EBlockExecutionStatus Status { get; }

    VariableBinding GetInput(string key);
    VariableBinding GetOutput(string key);
    VariableBinding GetInternalData(string key);
    Task<BlockExecutionResult> Execute(string triggerEvent,
        Func<Logic, CancellationToken, Task<bool>> EvaluateCondition,
        Func<Logic, CancellationToken, Task> RunAction,
        Func<IEnumerable<string>> GetOutputEvents,
        CancellationToken cancellationToken);
    void WaitForCompletion(CancellationToken cancellationToken);
}