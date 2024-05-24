namespace WELearning.Core.FunctionBlocks.Models.Design;

public class FunctionBlockProcess
{
    public FunctionBlockProcess(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Id { get; set; }
    public string Name { get; set; }

    public IEnumerable<FunctionBlock> Blocks { get; set; }
    public IEnumerable<BlockEventConnection> EventConnections { get; set; }
    public IEnumerable<BlockDataConnection> DataConnections { get; set; }
}