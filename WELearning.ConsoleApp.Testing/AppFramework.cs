using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Models.Runtime;

public class AppFrameworkInstance : BlockFrameworkInstance<AppFramework, AppFrameworkInstance>
{
    public AppFrameworkInstance(AppFramework blockFramework) : base(blockFramework)
    {
    }

    public double NextRandomDouble() => _blockFramework.NextRandomDouble();
}

public class AppFramework : BlockFramework<AppFrameworkInstance>
{
    public AppFramework(BlockExecutionControl control) : base(control)
    {
    }

    public override AppFrameworkInstance CreateInstance() => new AppFrameworkInstance(this);

    public double NextRandomDouble() => Random.Shared.NextDouble();
}

public class AppFrameworkFactory : IBlockFrameworkFactory<AppFrameworkInstance>
{
    public IBlockFramework<AppFrameworkInstance> Create(BlockExecutionControl control)
        => new AppFramework(control);
}