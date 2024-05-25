using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunProcessRequest
{
    public RunProcessRequest(FunctionBlockProcess process, HashSet<BlockTrigger> triggers = null)
    {
        Process = process;
        Triggers = triggers;
    }

    public FunctionBlockProcess Process { get; }
    public HashSet<BlockTrigger> Triggers { get; }
}