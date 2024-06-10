using Microsoft.Extensions.Logging;

namespace WELearning.Core.FunctionBlocks.Framework;

// [IMPORTANT] do not expose complex types cause Jint engine requires proxy which won't work for every types
public class FunctionFramework
{
    private readonly ILogger<FunctionFramework> _logger;

    public FunctionFramework(ILogger<FunctionFramework> logger)
    {
        _logger = logger;
    }

    public virtual Task DelayAsync(int ms) => Task.Delay(ms);
    public virtual void Delay(int ms) => DelayAsync(ms).Wait();

    public virtual void LogTrace(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogTrace(message);
    }

    public virtual void LogDebug(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogDebug(message);
    }

    public virtual void Log(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogInformation(message);
    }

    public virtual void LogError(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogError(message);
    }

    public virtual void LogWarning(params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.LogWarning(message);
    }

    public static string GetLogMessage(object[] data) => string.Join(' ', data);
}