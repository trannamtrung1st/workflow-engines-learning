namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockFrameworkFactory<TFramework>
{
    TFramework Create(IBlockExecutionControl control);
}