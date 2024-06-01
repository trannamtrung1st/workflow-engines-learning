using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

public class AppFramework : BlockFramework
{
    public AppFramework(IBasicEC control) : base(control)
    {
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();
}

public class AppFrameworkFactory : IBlockFrameworkFactory<AppFramework>
{
    public AppFramework Create(IBasicEC control) => new(control);
}