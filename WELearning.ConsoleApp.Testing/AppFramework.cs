using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Models.Runtime;

public class AppFramework : BlockFramework
{
    public AppFramework(BlockExecutionControl control) : base(control)
    {
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();
}

public class AppFrameworkFactory : IBlockFrameworkFactory<AppFramework>
{
    public AppFramework Create(BlockExecutionControl control) => new(control);
}