namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFramework<TFrameworkInstance>
{
    TFrameworkInstance CreateInstance();
}
