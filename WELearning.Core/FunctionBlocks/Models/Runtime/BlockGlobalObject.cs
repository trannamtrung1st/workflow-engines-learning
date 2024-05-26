namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject<TFrameworkInstance>
{
    public BlockGlobalObject(TFrameworkInstance frameworkInstance)
    {
        FB = frameworkInstance;
    }

    public TFrameworkInstance FB { get; }
}