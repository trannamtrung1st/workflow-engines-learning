using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockGlobalObject<TFunctionFramework>
{
    public BlockGlobalObject(TFunctionFramework framework,
        IBlockFramework blockFramework,
        IOutputEventPublisher publisher)
    {
        FB = framework;
        IN = blockFramework.InputBindings;
        OUT = blockFramework.OutputBindings;
        INOUT = blockFramework.InOutBindings;
        INTERNAL = blockFramework.InternalBindings;
        EVENTS = publisher;
    }

    public TFunctionFramework FB { get; }
    public IReadOnlyDictionary<string, IReadBinding> IN { get; }
    public IReadOnlyDictionary<string, IWriteBinding> OUT { get; }
    public IReadOnlyDictionary<string, IReadWriteBinding> INOUT { get; }
    public IReadOnlyDictionary<string, IReadWriteBinding> INTERNAL { get; }
    public IOutputEventPublisher EVENTS { get; }
}