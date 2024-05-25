using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessExecutionControl
{
    public ProcessExecutionControl()
    {
        ProcessTasks = new ConcurrentBag<BlockExecutionTaskInfo>();
        BlockExecutionControlMap = new ConcurrentDictionary<string, BlockExecutionControl>();
    }

    public ConcurrentBag<BlockExecutionTaskInfo> ProcessTasks { get; }
    public ConcurrentDictionary<string, BlockExecutionControl> BlockExecutionControlMap { get; }
    public virtual Exception Exception { get; protected internal set; }
    public virtual EProcessExecutionStatus Status { get; protected internal set; }
}