using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;

public class AppFramework : BlockFramework
{
    public AppFramework(IBlockExecutionControl control) : base(control)
    {
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();
}

public class AppFrameworkFactory : IBlockFrameworkFactory<AppFramework>
{
    public AppFramework Create(IBlockExecutionControl control) => new(control);
}