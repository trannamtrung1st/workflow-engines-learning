using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;

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

    protected override void Log(LogLevel logLevel, params object[] data)
    {
        var message = GetLogMessage(data);
        var currentStatement = _control.CurrentRunRequest?.Tracker.CurrentStatement;
        _logger.Log(logLevel, "[DEMO] Sending to editor...\n{Message} ({FunctionName}:{LineNumber})",
            message, currentStatement?.FunctionName, currentStatement?.LineNumber ?? -1);
    }

    public void DemoException() => throw new Exception("This is a sample .NET code exception!");
}