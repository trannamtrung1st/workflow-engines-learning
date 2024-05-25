namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class ProcessExecutionContext
{
    public ProcessExecutionContext(HashSet<ProcessVariableBinding> bindings)
    {
        Bindings = bindings;
    }

    public HashSet<ProcessVariableBinding> Bindings { get; }
}