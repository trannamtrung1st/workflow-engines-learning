using Microsoft.Extensions.Logging;
using WELearning.ConsoleApp.Testing.Framework.Bindings;
using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppBlockFramework : BlockFramework
{
    private readonly DataStore _dataStore;
    private readonly ILogger _logger;

    public AppBlockFramework(IExecutionControl control, DataStore dataStore, ILogger logger) : base(control, logger)
    {
        _dataStore = dataStore;
        _logger = logger;
    }

    public override object GetBindingFor(IValueObject valueObject)
    {
        if (valueObject is EntryValueObject entryValue)
        {
            var variable = entryValue.Variable;
            switch (variable.VariableType)
            {
                case EVariableType.Input: return new ReadEntryBinding(variable.Name, entryValue, _dataStore);
                default: return new EntryBinding(variable.Name, entryValue, _dataStore);
            }
        }
        else if (valueObject is MetricValueObject metricValue)
        {
            var variable = metricValue.Variable;
            switch (variable.VariableType)
            {
                case EVariableType.Input: return new ReadMetricBinding(variable.Name, metricValue, _dataStore);
                default: return new MetricBinding(variable.Name, metricValue, _dataStore);
            }
        }
        return base.GetBindingFor(valueObject);
    }

    private IFrameworkConsole _console;
    public override IFrameworkConsole GetFrameworkConsole() => _console ??= new AppFrameworkConsole(_logger, control: Control);
}
