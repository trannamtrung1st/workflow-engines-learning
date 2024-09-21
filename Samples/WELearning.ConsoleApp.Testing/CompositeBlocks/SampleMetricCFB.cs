using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.ConsoleApp.Testing.Framework.Bindings;
using WELearning.Core.FunctionBlocks.Helpers;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class SampleMetricCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "SampleMetric", name: "Sample CFB to demonstrate device metrics");

        var metricType = nameof(ReadMetricBinding);
        var iMetric = new Variable("Metric", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: metricType);
        var oSnapshot = new Variable("Snapshot", dataType: EDataType.Double, variableType: EVariableType.Output);
        var oPrevious = new Variable("Previous", dataType: EDataType.Double, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iMetric, oSnapshot, oPrevious };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iMetric.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oSnapshot.Name, oPrevious.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bLastSeriesBeforeDef = PredefinedBFBs.LastSeriesBeforeJs;
        var bMainDef = BlockHelper.CreateBlockSimple(
            id: "SampleMetricMain",
            name: "Sample metric main function",
            content:
            @$"
            const {{ Value, Timestamp }} = Metric.Snapshot;
            const {{ Result: prevSeries }} = await LastSeriesBefore({{ InputMetric: Metric, BeforeTime: Timestamp }}, {{ Result: null }});
            Snapshot = Value;
            Previous = prevSeries.Value;",
            runtime: ERuntime.Javascript, imports: new[] { $"import {{ LastSeriesBefore }} from '{FunctionDefaults.ModuleFunctions}'" },
            importModuleRefs: [new(
                Id: Guid.NewGuid().ToString(),
                ModuleName: FunctionDefaults.ModuleFunctions,
                BlockIds: [bLastSeriesBeforeDef.Id])],
            signature: null, exported: false,
            new Variable("Metric", EDataType.Reference, EVariableType.Input, objectType: metricType),
            new Variable("Snapshot", EDataType.Double, EVariableType.Output),
            new Variable("Previous", EDataType.Double, EVariableType.Output)
        );
        var bMain = new BlockInstance(bMainDef.Id, id: "Main");

        var bInputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Metric", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: metricType)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Snapshot", dataType: EDataType.Double, variableType: EVariableType.InOut),
            new Variable(name: "Previous", dataType: EDataType.Double, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bMain, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bMain.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bMain.Id,
                SourceEventName = "Completed"
            });

            // [NOTE] CFB output events
            eventConnections.Add(new(blockId: null, eventName: "Completed")
            {
                SourceBlockId = bOutputs.Id,
                SourceEventName = "Completed"
            });

            cfb.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockConnection>();

            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Snapshot", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMain.Id,
                SourceVariableName = "Snapshot"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Previous", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMain.Id,
                SourceVariableName = "Previous"
            });

            // [NOTE] CFB output data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Output || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: null, variableName: variable.Name, displayName: null, bindingType: EBindingType.Output)
                {
                    SourceBlockId = bOutputs.Id,
                    SourceVariableName = variable.Name
                });
            }

            cfb.DataConnections = dataConnections;
        }

        {
            var references = new List<BlockConnection>();
            references.Add(new(blockId: bInputs.Id, variableName: "Metric", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Metric"
            });
            references.Add(new(blockId: bMain.Id, variableName: "Metric", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Metric"
            });
            cfb.References = references;
        }

        cfb.MapDefinitions(new[] { bMainDef, bLastSeriesBeforeDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}