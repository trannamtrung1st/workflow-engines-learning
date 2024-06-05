using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFramework
{
    object GetBindingFor(IValueObject valueObject);
    Task HandleDynamicResult(dynamic result);
    IReadOnlyDictionary<string, IReadBinding> InputBindings { get; }
    IReadOnlyDictionary<string, IWriteBinding> OutputBindings { get; }
    IReadOnlyDictionary<string, IReadWriteBinding> InOutBindings { get; }
    IReadOnlyDictionary<string, IReadWriteBinding> InternalBindings { get; }
}
