using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessExecutionControl
{
    event EventHandler Running;
    event EventHandler<Exception> Failed;
    event EventHandler Completed;

    bool IsIdle { get; }
    FunctionBlockProcess Process { get; }
    int RunningTasksCount { get; }
    IEnumerable<BlockExecutionTaskInfo> ExecutionTasks { get; }
    Exception Exception { get; }
    EProcessExecutionStatus Status { get; }

    bool TryGetBlockControl(string blockId, out IBlockExecutionControl blockControl);
    Task Execute(IEnumerable<BlockTrigger> triggers, IEnumerable<ProcessVariableBinding> bindings, CancellationToken cancellationToken);
    Task MutexAccess(Func<Task> task, CancellationToken cancellationToken);
    void WaitForIdle(CancellationToken cancellationToken);
}