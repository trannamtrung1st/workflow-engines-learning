using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFrameworkFactory : IFunctionFrameworkFactory<AppFunctionFramework>
{
    public AppFunctionFramework Create(IBlockFramework blockFramework) => new(blockFramework);
}