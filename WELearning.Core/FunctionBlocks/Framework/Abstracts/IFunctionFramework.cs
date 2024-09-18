using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IFunctionFramework
{
    string VariableName { get; }

    IReadOnlyDictionary<string, object> GetReservedInputs();
    IFrameworkConsole GetFrameworkConsole();
    IReadBinding In(string name);
    IWriteBinding Out(string name);
    IReadWriteBinding InOut(string name);
    IReadWriteBinding Internal(string name);
    T Get<T>(string name, EVariableType variableType);
}
