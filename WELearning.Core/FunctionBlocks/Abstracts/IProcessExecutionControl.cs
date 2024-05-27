using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessExecutionControl
{
    FunctionBlockProcess Process { get; }
    int BlockRunningProcessCount { get; }
    IEnumerable<BlockExecutionTaskInfo> ExecutionTasks { get; }
    Exception Exception { get; }
    EProcessExecutionStatus Status { get; }
    void WaitForCompletion(CancellationToken cancellationToken);
    IBlockExecutionControl GetBlockControl(string blockId);
    IBlockExecutionControl GetOrInitBlockControl(FunctionBlockInstance block);
    Task Run(RunProcessRequest request, Func<RunBlockRequest, IBlockExecutionControl, CancellationToken, Task<BlockExecutionResult>> RunBlock, CancellationToken cancellationToken);
}