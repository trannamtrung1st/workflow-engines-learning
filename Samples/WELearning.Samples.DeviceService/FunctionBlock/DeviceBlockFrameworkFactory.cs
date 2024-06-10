using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Samples.DeviceService.Services.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class DeviceBlockFrameworkFactory : BlockFrameworkFactory
{
    private readonly IAssetService _assetService;
    public DeviceBlockFrameworkFactory(IAssetService assetService)
    {
        _assetService = assetService;
    }

    public override IBlockFramework Create(IExecutionControl control)
        => new DeviceBlockFramework(control, _assetService);
}