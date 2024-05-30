using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessExecutionControl
{
    event EventHandler Running;
    event EventHandler<Exception> Failed;
    event EventHandler Completed;

    bool IsRunning { get; }
    FunctionBlockProcess Process { get; }
    int RunningTasksCount { get; }
    IEnumerable<BlockExecutionTaskInfo> ExecutionTasks { get; }
    Exception Exception { get; }
    EProcessExecutionStatus Status { get; }

    void WaitForCompletion(CancellationToken cancellationToken);
    bool TryGetBlockControl(string blockId, out IBlockExecutionControl blockControl);
    Task Execute(RunProcessRequest request, Func<RunBlockRequest, IBlockExecutionControl, CancellationToken, Task<BlockExecutionResult>> RunBlock, CancellationToken cancellationToken);
}