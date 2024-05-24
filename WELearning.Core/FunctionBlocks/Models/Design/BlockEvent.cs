namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockEvent
{
    public BlockEvent(string name, IEnumerable<string> variableNames)
    {
        Name = name;
        VariableNames = variableNames;
    }

    public string Name { get; set; }
    public IEnumerable<string> VariableNames { get; set; }
}