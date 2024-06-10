using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.ConsoleApp.Testing.Framework;

public class DeviceFunctionFramework : FunctionFramework
{
    private readonly ILogger<DeviceFunctionFramework> _logger;
    public DeviceFunctionFramework(ILogger<DeviceFunctionFramework> logger) : base(logger)
    {
        _logger = logger;
    }
}