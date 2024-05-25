namespace WELearning.Core.FunctionBlocks.Models.Design;

public class FunctionBlock
{
    public FunctionBlock(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; }
    public string Name { get; set; }

    public BlockExecutionControlChart ExecutionControlChart { get; set; }

    public string DefaultTriggerEvent { get; set; }
    public IEnumerable<BlockEvent> InputEvents { get; set; }
    public IEnumerable<BlockEvent> OutputEvents { get; set; }

    public IEnumerable<Variable> Inputs { get; set; }
    public IEnumerable<Variable> Outputs { get; set; }

    public IEnumerable<Logic> Logics { get; set; }
}