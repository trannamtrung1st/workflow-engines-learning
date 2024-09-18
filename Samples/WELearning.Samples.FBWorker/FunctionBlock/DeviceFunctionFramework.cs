using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceFunctionFramework : FunctionFramework
{
    public DeviceFunctionFramework(IBlockFramework blockFramework, ILogger<DeviceFunctionFramework> logger) : base(blockFramework, logger)
    {
    }
}