using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFramework(IBlockFramework blockFramework) : FunctionFramework(blockFramework)
{
    public double NextRandomDouble() => Random.Shared.NextDouble();
    public void DemoException() => throw new Exception("This is a sample .NET code exception!");
}
