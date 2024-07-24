using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

// [IMPORTANT] do not expose complex types cause Jint engine requires proxy which won't work for every types
public class FunctionFramework : IFunctionFramework
{
    private readonly ILogger<FunctionFramework> _logger;

    public FunctionFramework(ILogger<FunctionFramework> logger)
    {
        _logger = logger;
    }

    public virtual string VariableName => "FB";
    public virtual Task DelayAsync(int ms) => Task.Delay(ms);
    public virtual void Delay(int ms) => DelayAsync(ms).Wait();

    protected virtual void Log(LogLevel logLevel, params object[] data)
    {
        var message = GetLogMessage(data);
        _logger.Log(logLevel, message);
    }

    public virtual void LogTrace(params object[] data) => Log(LogLevel.Trace, data);

    public virtual void LogDebug(params object[] data) => Log(LogLevel.Debug, data);

    public virtual void Log(params object[] data) => Log(LogLevel.Information, data);

    public virtual void LogError(params object[] data) => Log(LogLevel.Error, data);

    public virtual void LogWarning(params object[] data) => Log(LogLevel.Warning, data);

    public static string GetLogMessage(object[] data) => string.Join(' ', data);

    public void Terminate(bool graceful = true, string message = null) => throw new BlockTerminatedException(graceful, message);
}