using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceFunctionFrameworkFactory : IFunctionFrameworkFactory<DeviceFunctionFramework>
{
    private readonly ILogger<DeviceFunctionFramework> _logger;
    public DeviceFunctionFrameworkFactory(ILogger<DeviceFunctionFramework> logger)
    {
        _logger = logger;
    }

    public DeviceFunctionFramework Create(IBlockFramework blockFramework) => new(blockFramework, _logger);
}