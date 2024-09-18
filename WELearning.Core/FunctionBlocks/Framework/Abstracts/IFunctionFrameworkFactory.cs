namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IFunctionFrameworkFactory<TFunctionFramework> where TFunctionFramework : IFunctionFramework
{
    TFunctionFramework Create(IBlockFramework blockFramework);
}