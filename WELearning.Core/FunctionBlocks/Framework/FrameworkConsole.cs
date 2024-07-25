using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public class FrameworkConsole : IFrameworkConsole
{
    private readonly ILogger _logger;

    public FrameworkConsole(ILogger logger)
    {
        _logger = logger;
    }

    protected virtual void Log(LogLevel logLevel, params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.Log(logLevel, message);
    }

    public virtual void Trace(params object[] data) => Log(LogLevel.Trace, data);

    public virtual void Debug(params object[] data) => Log(LogLevel.Debug, data);

    public virtual void Log(params object[] data) => Log(LogLevel.Information, data);

    public virtual void Error(params object[] data) => Log(LogLevel.Error, data);

    public virtual void Warn(params object[] data) => Log(LogLevel.Warning, data);

    public static string GetLogMessage(object[] data) => string.Join(' ', data);
}