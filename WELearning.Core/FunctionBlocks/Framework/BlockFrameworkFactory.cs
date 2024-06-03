using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkFactory : IBlockFrameworkFactory<IBlockFramework>
{
    private readonly ILogger<BlockFramework> _logger;

    public BlockFrameworkFactory(ILogger<BlockFramework> logger)
    {
        _logger = logger;
    }

    public IBlockFramework Create(IExecutionControl control) => new BlockFramework(control, _logger);
}
