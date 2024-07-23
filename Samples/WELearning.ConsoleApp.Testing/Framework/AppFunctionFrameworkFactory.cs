using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFrameworkFactory : IFunctionFrameworkFactory<AppFunctionFramework>
{
    private readonly ILogger<AppFunctionFramework> _logger;

    public AppFunctionFrameworkFactory(ILogger<AppFunctionFramework> logger)
    {
        _logger = logger;
    }

    public AppFunctionFramework Create(IExecutionControl control) => new(control, _logger);
}