using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFrameworkFactory(ILogger<IExecutionControl> logger) : IBlockFrameworkFactory
{
    public virtual IBlockFramework Create(IExecutionControl control) => new BlockFramework(control, logger);
}
