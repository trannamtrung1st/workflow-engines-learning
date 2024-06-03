using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ICompositeEC : IExecutionControl
{
    event EventHandler ControlRunning;
    event EventHandler ControlCompleted;
    event EventHandler<Exception> ControlFailed;

    CompositeBlockDef Definition { get; }
    bool TryGetExecutionControl(string blockId, out IExecutionControl executionControl);
}