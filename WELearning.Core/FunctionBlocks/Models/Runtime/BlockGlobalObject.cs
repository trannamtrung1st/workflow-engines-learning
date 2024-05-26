namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject<TFramework>
{
    public BlockGlobalObject(TFramework framework)
    {
        FB = framework;
    }

    public TFramework FB { get; }
}