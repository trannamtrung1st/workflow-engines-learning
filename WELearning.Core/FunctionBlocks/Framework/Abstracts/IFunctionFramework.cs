namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IFunctionFramework
{
    string VariableName { get; }

    IReadOnlyDictionary<string, object> GetReservedInputs();
    IFrameworkConsole GetFrameworkConsole();
}
