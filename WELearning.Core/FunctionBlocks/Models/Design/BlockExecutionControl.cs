namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockExecutionControl
{
    public BlockState InitialState { get; set; }
    public IEnumerable<BlockState> States { get; set; }
    public IEnumerable<BlockStateTransition> StateTransitions { get; set; }
}