namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BlockEvent
{
    public BlockEvent(bool isInput, string name, IEnumerable<string> variableNames)
    {
        IsInput = isInput;
        Name = name;
        VariableNames = variableNames;
    }

    public bool IsInput { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> VariableNames { get; set; }
}