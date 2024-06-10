using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFramework : FunctionFramework
{
    private readonly ILogger<AppFunctionFramework> _logger;
    public AppFunctionFramework(ILogger<AppFunctionFramework> logger) : base(logger)
    {
        _logger = logger;
    }

    public double NextRandomDouble() => Random.Shared.NextDouble();

    public override void LogTrace(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogTrace("[DEMO] Sending to editor...\n{Message}", message);
    }

    public override void LogDebug(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogDebug("[DEMO] Sending to editor...\n{Message}", message);
    }

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