using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

public class AppFramework : BlockFramework
{
    private readonly ILogger<AppFramework> _logger;
    public AppFramework(IExecutionControl control, ILogger<AppFramework> logger) : base(control, logger)
    {
        _logger = logger;
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();

    public override void Log(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogInformation("[DEMO] Sending to editor...\n{Message}", message);
    }

    public override void LogError(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogError("[DEMO] Sending to editor...\n{Message}", message);
    }

    public override void LogWarning(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogWarning("[DEMO] Sending to editor...\n{Message}", message);
    }

    public void DemoException() => throw new Exception("This is a sample .NET code exception!");
}

public class AppFrameworkFactory : IBlockFrameworkFactory<AppFramework>
{
    private readonly ILogger<AppFramework> _logger;
    public AppFrameworkFactory(ILogger<AppFramework> logger)
    {
        _logger = logger;
    }

    public AppFramework Create(IExecutionControl control) => new(control, _logger);
}