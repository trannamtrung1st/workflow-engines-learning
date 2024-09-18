using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockFramework
{
    IExecutionControl Control { get; }

    object GetBindingFor(IValueObject valueObject);
    object GetBindingFor(string name, EVariableType eVariableType);
    void HandleDynamicResult(dynamic result);
    IOutputEventPublisher CreateEventPublisher(HashSet<string> outputEvents);
}
