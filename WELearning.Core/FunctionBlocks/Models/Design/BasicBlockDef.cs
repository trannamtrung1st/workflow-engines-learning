namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BasicBlockDef : BaseBlockDef
{
    public BasicBlockDef(string id, string name) : base(id, name)
    {
    }

    public BlockECC ExecutionControlChart { get; set; }
    public IEnumerable<Function> Functions { get; set; }
}