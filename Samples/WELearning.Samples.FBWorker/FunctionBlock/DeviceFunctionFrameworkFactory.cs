using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceFunctionFrameworkFactory : IFunctionFrameworkFactory<DeviceFunctionFramework>
{
    public DeviceFunctionFramework Create(IBlockFramework blockFramework) => new(blockFramework);
}