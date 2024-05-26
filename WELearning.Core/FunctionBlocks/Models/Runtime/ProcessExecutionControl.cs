using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessExecutionControl<TFrameworkInstance>
{
    public ProcessExecutionControl()
    {
        ProcessTasks = new();
        ExecutionTasks = new();
        BlockExecutionMap = new();
    }

    public ConcurrentBag<Task> ProcessTasks { get; }
    public ConcurrentBag<BlockExecutionTaskInfo> ExecutionTasks { get; }
    public ConcurrentDictionary<string, (BlockExecutionControl Control, IBlockFramework<TFrameworkInstance> Framework)> BlockExecutionMap { get; }
    public virtual Exception Exception { get; protected internal set; }
    public virtual EProcessExecutionStatus Status { get; protected internal set; }
}