namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockECC
{
    public string InitialState { get; set; }
    public IEnumerable<BlockState> States { get; set; }
    public IEnumerable<BlockStateTransition> StateTransitions { get; set; }
}