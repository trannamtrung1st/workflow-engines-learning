using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject<TFunctionFramework> : IArguments where TFunctionFramework : IFunctionFramework
{
    private readonly Dictionary<string, object> _arguments;

    public BlockGlobalObject(
        IBlockFramework blockFramework,
        TFunctionFramework functionFramework,
        IOutputEventPublisher publisher,
        IReadOnlyDictionary<string, object> reservedInputs)
    {
        FB = functionFramework;
        EVENTS = publisher;
        CONSOLE = blockFramework.GetFrameworkConsole();

        _arguments = new()
        {
            [FB.VariableName] = FB,
            [BuiltInVariables.EVENTS] = EVENTS,
        };

        reservedInputs?.AssignTo(_arguments);
        blockFramework.GetReservedInputs()?.AssignTo(_arguments);
    }

    public TFunctionFramework FB { get; }
    public IOutputEventPublisher EVENTS { get; }

    // [NOTE] for CS only
    public IFrameworkConsole CONSOLE { get; }

    public IReadOnlyDictionary<string, object> GetArguments() => _arguments;
}