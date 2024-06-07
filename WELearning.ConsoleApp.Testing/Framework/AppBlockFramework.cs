using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppBlockFramework : BlockFramework
{
    private readonly DataStore _dataStore;
    public AppBlockFramework(IExecutionControl control, DataStore dataStore) : base(control)
    {
        _dataStore = dataStore;
    }

    public override object GetBindingFor(IValueObject valueObject)
    {
        if (valueObject is not EntryValueObject entryValue)
            return base.GetBindingFor(valueObject);
        var variable = entryValue.Variable;
        switch (variable.VariableType)
        {
            case EVariableType.Input: return new ReadEntryBinding(variable.Name, entryValue, _dataStore);
            default: return new EntryBinding(variable.Name, entryValue, _dataStore);
        }
    }
}
