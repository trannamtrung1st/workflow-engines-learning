using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

// [IMPORTANT] do not expose complex types cause Jint engine requires proxy which won't work for every types
public class FunctionFramework : IFunctionFramework
{
    public static class ReservedInputs
    {
        public const string Console = "console";
    }

    private readonly ILogger _logger;
    public FunctionFramework(ILogger logger)
    {
        _logger = logger;
    }

    public virtual string VariableName => "FB";
    public virtual Task DelayAsync(int ms) => Task.Delay(ms);
    public virtual void Delay(int ms) => DelayAsync(ms).Wait();
    public virtual void Terminate(bool graceful = true, string message = null) => throw new BlockTerminatedException(graceful, message);

    private IReadOnlyDictionary<string, object> _reservedInputs;
    public virtual IReadOnlyDictionary<string, object> GetReservedInputs() => _reservedInputs ??= new Dictionary<string, object>()
    {
        [ReservedInputs.Console] = GetFrameworkConsole()
    };

    private IFrameworkConsole _console;
    public virtual IFrameworkConsole GetFrameworkConsole() => _console ??= new FrameworkConsole(_logger);
}
