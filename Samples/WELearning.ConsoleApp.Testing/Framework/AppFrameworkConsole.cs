using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppFrameworkConsole : FrameworkConsole
{
    private readonly ILogger _logger;
    private readonly IExecutionControl _control;

    public AppFrameworkConsole(ILogger logger, IExecutionControl control) : base(logger)
    {
        _logger = logger;
        _control = control;
    }

    protected override void Log(LogLevel logLevel, params object[] data)
    {
        var message = GetLogMessage(data);
        var currentStatement = _control.CurrentRunRequest?.Tracker.CurrentStatement;
        _logger.Log(logLevel, "[{LogLevel}] Sending to editor...\n{Message} ({FunctionName}:{LineNumber})",
            logLevel.ToString(), message, currentStatement?.FunctionName, currentStatement?.LineNumber ?? -1);
    }
}