using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class RunProcessRequest
{
    public RunProcessRequest(FunctionBlockProcess process, IEnumerable<ProcessVariableBinding> bindings, IEnumerable<BlockTrigger> triggers = null)
    {
        Process = process;
        Triggers = triggers;
        Bindings = bindings;
    }

    public FunctionBlockProcess Process { get; }
    public IEnumerable<BlockTrigger> Triggers { get; }
    public IEnumerable<ProcessVariableBinding> Bindings { get; }
}