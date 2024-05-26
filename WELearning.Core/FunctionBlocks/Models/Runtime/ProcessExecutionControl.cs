using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessExecutionControl<TFramework>
{
    private readonly ManualResetEventSlim _processIdleWait;

    public ProcessExecutionControl()
    {
        ExecutionTasks = new();
        BlockExecutionControlMap = new();
        _blockRunningProcessCount = 0;
        _processIdleWait = new ManualResetEventSlim();
    }

    private int _blockRunningProcessCount;
    public virtual int BlockRunningProcessCount => _blockRunningProcessCount;
    public ConcurrentBag<BlockExecutionTaskInfo> ExecutionTasks { get; }
    public ConcurrentDictionary<string, BlockExecutionControl> BlockExecutionControlMap { get; }
    public virtual Exception Exception { get; protected internal set; }
    public virtual EProcessExecutionStatus Status { get; protected internal set; }

    public virtual void StartProcess()
    {
        lock (_processIdleWait)
        {
            _blockRunningProcessCount++;
            _processIdleWait.Reset();
        }
    }

    public virtual void CompleteProcess()
    {
        lock (_processIdleWait)
        {
            if (_blockRunningProcessCount == 0) return;
            _blockRunningProcessCount--;
            if (_blockRunningProcessCount == 0)
                _processIdleWait.Set();
        }
    }

    public virtual void WaitForCompletion() => _processIdleWait.Wait();

    public virtual async Task<BlockExecutionTaskInfo> WaitForCompletion(string blockId)
    {
        var blockRunningTask = ExecutionTasks
            .Where(t => t.BlockId == blockId)
            .OrderByDescending(t => t.StartTime)
            .FirstOrDefault();
        if (blockRunningTask != null && blockRunningTask.CompletedTime == null)
            await blockRunningTask.ExecutionTask;
        return blockRunningTask;
    }
}