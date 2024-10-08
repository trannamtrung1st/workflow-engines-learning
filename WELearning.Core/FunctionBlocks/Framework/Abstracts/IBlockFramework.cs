using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFramework
{
    IExecutionControl Control { get; }

    IReadOnlyDictionary<string, object> GetReservedInputs();
    IFrameworkConsole GetFrameworkConsole();
    object GetBindingFor(IValueObject valueObject);
    object GetBindingFor(string name, EVariableType eVariableType);
    void HandleDynamicResult(dynamic result, Function function);
    IOutputEventPublisher CreateEventPublisher(HashSet<string> outputEvents);
}
