using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject<TFunctionFramework> : IArguments where TFunctionFramework : IFunctionFramework
{
    private readonly Dictionary<string, object> _arguments;

    public BlockGlobalObject(
        TFunctionFramework framework,
        IBlockFramework blockFramework,
        IOutputEventPublisher publisher,
        IReadOnlyDictionary<string, object> reservedInputs)
    {
        FB = framework;
        IN = blockFramework.InputBindings;
        OUT = blockFramework.OutputBindings;
        INOUT = blockFramework.InOutBindings;
        INTERNAL = blockFramework.InternalBindings;
        RESERVED = reservedInputs;
        EVENTS = publisher;

        _arguments = new()
        {
            [FB.VariableName] = FB,
            [BuiltInVariables.IN] = IN,
            [BuiltInVariables.OUT] = OUT,
            [BuiltInVariables.INOUT] = INOUT,
            [BuiltInVariables.INTERNAL] = INTERNAL,
            [BuiltInVariables.RESERVED] = RESERVED,
            [BuiltInVariables.EVENTS] = EVENTS,
        };
    }

    public TFunctionFramework FB { get; }
    public IReadOnlyDictionary<string, IReadBinding> IN { get; }
    public IReadOnlyDictionary<string, IWriteBinding> OUT { get; }
    public IReadOnlyDictionary<string, IReadWriteBinding> INOUT { get; }
    public IReadOnlyDictionary<string, IReadWriteBinding> INTERNAL { get; }
    public IReadOnlyDictionary<string, object> RESERVED { get; }
    public IOutputEventPublisher EVENTS { get; }

    public IReadOnlyDictionary<string, object> GetArguments() => _arguments;
}