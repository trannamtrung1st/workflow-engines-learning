using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFramework : FunctionFramework
{
    private readonly IExecutionControl _control;
    private readonly ILogger<AppFunctionFramework> _logger;

    public AppFunctionFramework(IExecutionControl control, ILogger<AppFunctionFramework> logger) : base(logger)
    {
        _control = control;
        _logger = logger;
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();
    public void DemoException() => throw new Exception("This is a sample .NET code exception!");

    private IFrameworkConsole _console;
    public override IFrameworkConsole GetFrameworkConsole() => _console ??= new AppFrameworkConsole(_logger, _control);
}
