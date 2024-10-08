using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Samples.FBWorker.Services.Abstracts;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceBlockFrameworkFactory : BlockFrameworkFactory
{
    private readonly IAssetService _assetService;
    private readonly ILogger<IExecutionControl> _logger;

    public DeviceBlockFrameworkFactory(IAssetService assetService, ILogger<IExecutionControl> logger) : base(logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    public override IBlockFramework Create(IExecutionControl control)
        => new DeviceBlockFramework(control, _assetService, _logger);
}