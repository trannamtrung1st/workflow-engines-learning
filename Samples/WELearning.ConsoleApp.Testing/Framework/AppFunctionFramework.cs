using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFunctionFramework : FunctionFramework
{
    public const string MainFunctionName = "main";

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
        var functionName =
            (currentStatement?.FunctionName == null || currentStatement?.FunctionName == JsEngineConstants.WrapFunction)
            ? MainFunctionName : currentStatement?.FunctionName;
        _logger.Log(logLevel, "[DEMO] Sending to editor...\n{Message} ({FunctionName}:{LineNumber})",
            message, functionName, currentStatement?.LineNumber ?? -1);
    }

    public void DemoException() => throw new Exception("This is a sample .NET code exception!");
}