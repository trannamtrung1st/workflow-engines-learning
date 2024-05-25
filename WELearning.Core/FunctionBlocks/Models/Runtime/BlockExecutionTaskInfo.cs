namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionTaskInfo
{
    public BlockExecutionTaskInfo(string blockId, DateTime startTime, Task<BlockExecutionResult> executionTask)
    {
        BlockId = blockId;
        StartTime = startTime;
        ExecutionTask = executionTask.ContinueWith((task) =>
        {
            Result = task.Result;
            CompletedTime = DateTime.UtcNow;
        }, continuationOptions: TaskContinuationOptions.NotOnFaulted);
    }

    public string BlockId { get; }
    public DateTime StartTime { get; }
    public Task ExecutionTask { get; }
    public BlockExecutionResult Result { get; protected set; }
    public DateTime? CompletedTime { get; protected set; }
}