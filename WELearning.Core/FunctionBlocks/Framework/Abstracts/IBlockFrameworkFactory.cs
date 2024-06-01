using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFrameworkFactory<TFramework>
{
    TFramework Create(IBasicEC control);
}