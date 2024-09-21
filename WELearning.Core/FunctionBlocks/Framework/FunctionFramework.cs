using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Constants;
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
    protected readonly IBlockFramework blockFramework;

    public FunctionFramework(IBlockFramework blockFramework, ILogger logger)
    {
        this.blockFramework = blockFramework;
        _logger = logger;
    }

    public virtual string VariableName => "FB";
    public virtual Task DelayAsync(int ms) => Task.Delay(ms);
    public virtual void Delay(int ms) => DelayAsync(ms).Wait();
    public virtual void Terminate(bool graceful = true, string message = null) => throw new ManuallyTerminatedException(graceful, message);

    private IReadOnlyDictionary<string, object> _reservedInputs;
    public virtual IReadOnlyDictionary<string, object> GetReservedInputs() => _reservedInputs ??= new Dictionary<string, object>()
    {
        [ReservedInputs.Console] = GetFrameworkConsole()
    };

    private IFrameworkConsole _console;
    public virtual IFrameworkConsole GetFrameworkConsole() => _console ??= new FrameworkConsole(_logger);

    public IReadBinding In(string name) => (IReadBinding)blockFramework.GetBindingFor(name, EVariableType.Input);

    public IWriteBinding Out(string name) => (IWriteBinding)blockFramework.GetBindingFor(name, EVariableType.Output);

    public IReadWriteBinding InOut(string name) => (IReadWriteBinding)blockFramework.GetBindingFor(name, EVariableType.InOut);

    public IReadWriteBinding Internal(string name) => (IReadWriteBinding)blockFramework.GetBindingFor(name, EVariableType.Internal);

    public T Get<T>(string name, EVariableType variableType) => (T)blockFramework.GetBindingFor(name, variableType);
}
