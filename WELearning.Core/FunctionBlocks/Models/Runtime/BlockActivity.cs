using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockActivity
{
    public BlockActivity(ICompositeEC control, RunBlockRequest runRequest = null, DateTime timeUtc = default)
        : this((IExecutionControl)control, runRequest, timeUtc)
    {
        ExceptionFrom = control.ExceptionFrom;
    }

    public BlockActivity(IBasicEC control, RunBlockRequest runRequest = null, DateTime timeUtc = default)
        : this((IExecutionControl)control, runRequest, timeUtc)
    {
        RunningFunction = control.RunningFunction;
    }

    public BlockActivity(IExecutionControl control, RunBlockRequest runRequest = null, DateTime timeUtc = default)
    {
        Control = control;
        Status = control.Status;
        TimeUtc = timeUtc == default ? DateTime.UtcNow : timeUtc;
        Exception = control.Exception;
        Result = control.Result;
        RunRequest = runRequest;
        CalculateRuntime(control.LastActivity);
    }

    public Function RunningFunction { get; }
    public RunBlockRequest RunRequest { get; }
    public IExecutionControl Control { get; }
    public EBlockExecutionStatus Status { get; }
    public DateTime TimeUtc { get; }
    public Exception Exception { get; }
    public IExecutionControl ExceptionFrom { get; }
    public BlockExecutionResult Result { get; }
    public TimeSpan? RunTime { get; private set; }

    private void CalculateRuntime(BlockActivity lastActivity)
    {
        if (
            (Status != EBlockExecutionStatus.Completed && Status != EBlockExecutionStatus.Failed)
            || lastActivity?.Status != EBlockExecutionStatus.Running
        ) return;
        RunTime = TimeUtc - lastActivity.TimeUtc;
    }
}