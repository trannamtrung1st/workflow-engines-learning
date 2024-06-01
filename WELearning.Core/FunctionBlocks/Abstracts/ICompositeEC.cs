using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ICompositeEC : IExecutionControl
{
    CompositeBlockDef Definition { get; }
    bool TryGetExecutionControl(string blockId, out IExecutionControl executionControl);
}