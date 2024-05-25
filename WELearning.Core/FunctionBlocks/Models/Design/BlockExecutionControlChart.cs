namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockExecutionControlChart
{
    public string InitialState { get; set; }
    public IEnumerable<BlockState> States { get; set; }
    public IEnumerable<BlockStateTransition> StateTransitions { get; set; }
}