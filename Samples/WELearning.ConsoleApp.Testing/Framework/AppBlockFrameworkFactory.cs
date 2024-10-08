using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppBlockFrameworkFactory : BlockFrameworkFactory
{
    private readonly DataStore _dataStore;
    private readonly ILogger<IExecutionControl> _logger;

    public AppBlockFrameworkFactory(DataStore dataStore, ILogger<IExecutionControl> logger) : base(logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public override IBlockFramework Create(IExecutionControl control) => new AppBlockFramework(control, _dataStore, _logger);
}