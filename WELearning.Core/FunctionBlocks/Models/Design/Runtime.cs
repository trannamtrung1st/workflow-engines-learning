using WELearning.Core.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Runtime : Dictionary<string, object>
{
    public ERuntime Type { get; set; }
}

public class CSharpScriptRuntime : Runtime
{
    public CSharpScriptRuntime(IEnumerable<string> usings)
    {
        Type = ERuntime.CSharpScript;
        Usings = usings;
    }

    public IEnumerable<string> Usings
    {
        get => this[nameof(Usings)] as IEnumerable<string>;
        set => this[nameof(Usings)] = value;
    }
}