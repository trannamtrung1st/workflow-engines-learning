using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.ConsoleApp.Testing.Entities;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class EntryReportCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "EntryReport", name: "Entry Report: a sample process using external object reference");

        var iTemp = new Variable("Temperature", dataType: EDataType.Reference, variableType: EVariableType.Input);
        var iHumidity = new Variable("Humidity", dataType: EDataType.Reference, variableType: EVariableType.Input);
        var iReport = new Variable("Report", dataType: EDataType.Reference, variableType: EVariableType.Input);
        var oReport = new Variable("Report", dataType: EDataType.Reference, variableType: EVariableType.Output);
        var oFinalReport = new Variable("FinalReport", dataType: EDataType.Reference, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iTemp, iHumidity, iReport, oReport, oFinalReport };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iTemp.Name, iHumidity.Name, iReport.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oFinalReport.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bConcatDef = PredefinedBFBs.ConcatTwoStringsJs;
        var bConcat1 = new BlockInstance(bConcatDef.Id, id: "Concat1");
        var bConcat2 = new BlockInstance(bConcatDef.Id, id: "Concat2");

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Temperature", dataType: EDataType.Reference, variableType: EVariableType.InOut),
            new Variable(name: "Humidity", dataType: EDataType.Reference, variableType: EVariableType.InOut),
            new Variable(name: "Report", dataType: EDataType.Reference, variableType: EVariableType.InOut),
            new Variable(name: "Delimiter", dataType: EDataType.String, variableType: EVariableType.InOut, defaultValue: " "),
            new Variable(name: "FinalPrefix", dataType: EDataType.String, variableType: EVariableType.InOut, defaultValue: "FINAL:")
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var entryType = nameof(EntryEntity);
        var bReportDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Report", dataType: EDataType.Reference, variableType: EVariableType.InOut, detailedType: entryType)
        );
        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "FinalReport", dataType: EDataType.Reference, variableType: EVariableType.InOut, detailedType: entryType)
        );
        var bReport = new BlockInstance(definitionId: bReportDef.Id, id: "ReportEntry");
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bConcat1, bConcat2, bInputs, bReport, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bConcat1.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bReport.Id, eventName: "Trigger")
            {
                SourceBlockId = bConcat1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bConcat2.Id, eventName: "Trigger")
            {
                SourceBlockId = bReport.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bConcat2.Id,
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
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bConcat1.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Temperature"
            });
            dataConnections.Add(new(blockId: bConcat1.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Humidity"
            });
            dataConnections.Add(new(blockId: bConcat1.Id, variableName: "Delimiter", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Delimiter"
            });
            dataConnections.Add(new(blockId: bReport.Id, variableName: "Report", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bConcat1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bConcat2.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "FinalPrefix"
            });
            dataConnections.Add(new(blockId: bConcat2.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bReport.Id,
                SourceVariableName = "Report"
            });
            dataConnections.Add(new(blockId: bConcat2.Id, variableName: "Delimiter", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Delimiter"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "FinalReport", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bConcat2.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: null, variableName: "Report", displayName: null, bindingType: EBindingType.Output)
            {
                SourceBlockId = bOutputs.Id,
                SourceVariableName = "Report"
            });
            cfb.DataConnections = dataConnections;
        }

        {
            var references = new List<BlockReference>();
            references.Add(new(blockId: bInputs.Id, variableName: "Temperature", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Temperature"
            });
            references.Add(new(blockId: bInputs.Id, variableName: "Humidity", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Humidity"
            });
            references.Add(new(blockId: bInputs.Id, variableName: "Report", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Report"
            });
            references.Add(new(blockId: bReport.Id, variableName: "Report", displayName: null, bindingType: EBindingType.Output)
            {
                SourceVariableName = "Report"
            });
            references.Add(new(blockId: bOutputs.Id, variableName: "FinalReport", displayName: null, bindingType: EBindingType.Output)
            {
                SourceVariableName = "FinalReport"
            });
            cfb.References = references;
        }

        cfb.MapDefinitions(new[] { bConcatDef, bReportDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}