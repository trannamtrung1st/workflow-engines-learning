using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.ConsoleApp.Testing.Framework.Bindings;
using WELearning.Core.FunctionBlocks.Helpers;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class EntryReportCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "EntryReport", name: "Entry Report: a sample process using external object reference");

        var entryType = nameof(EntryBinding);
        var iTemp = new Variable("Temperature", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: entryType);
        var iHumidity = new Variable("Humidity", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: entryType);
        var iReport = new Variable("Report", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: entryType);
        var oReport = new Variable("Report", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: entryType);
        var oFinalReport = new Variable("FinalReport", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: entryType);
        cfb.Variables = new Variable[] { iTemp, iHumidity, iReport, oReport, oFinalReport };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iTemp.Name, iHumidity.Name, iReport.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oFinalReport.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bPrependDef = PredefinedBFBs.PrependEntryJs;
        var bConcatDef = PredefinedBFBs.ConcatTwoStringsJs;
        var bCustomConcatDef = BlockHelper.CreateBlockSimple(
            id: "CustomConcat",
            name: "Sample using reference binding method",
            content: @$"
            const prependResult = PrependEntry({{ InputEntry: Entry, OtherName: OtherEntryName }}, {{ Result }})
            Result = prependResult.Result",
            runtime: ERuntime.Javascript, imports: new[] { $"import {{ PrependEntry }} from '{FunctionDefaults.ModuleFunctions}'" },
            importModuleRefs: [new(
                Id: Guid.NewGuid().ToString(),
                ModuleName: FunctionDefaults.ModuleFunctions,
                BlockIds: [bPrependDef.Id])],
            signature: null, exported: false,
            new Variable("Entry", EDataType.Reference, EVariableType.Input, objectType: entryType),
            new Variable("OtherEntryName", EDataType.String, EVariableType.Input),
            new Variable("Result", EDataType.String, EVariableType.Output)
        );
        var bConcat1 = new BlockInstance(bConcatDef.Id, id: "Concat1");
        var bConcat2 = new BlockInstance(bCustomConcatDef.Id, id: "Concat2");

        var bInputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Temperature", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: entryType),
            new Variable(name: "Humidity", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: entryType),
            new Variable(name: "Report", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: entryType),
            new Variable(name: "Delimiter", dataType: EDataType.String, variableType: EVariableType.InOut, defaultValue: " "),
            new Variable(name: "OtherEntryName", dataType: EDataType.String, variableType: EVariableType.InOut, defaultValue: "FinalPrefix")
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bReportDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Report", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: entryType)
        );
        var bOutputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "FinalReport", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: entryType)
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
            var dataConnections = new List<BlockConnection>();
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
            dataConnections.Add(new(blockId: bConcat2.Id, variableName: "Entry", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Report"
            });
            dataConnections.Add(new(blockId: bConcat2.Id, variableName: "OtherEntryName", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "OtherEntryName"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "FinalReport", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bConcat2.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: null, variableName: "FinalReport", displayName: null, bindingType: EBindingType.Output)
            {
                SourceBlockId = bOutputs.Id,
                SourceVariableName = "FinalReport"
            });
            cfb.DataConnections = dataConnections;
        }

        {
            var references = new List<BlockConnection>();
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

        cfb.MapDefinitions(new[] { bConcatDef, bCustomConcatDef, bPrependDef, bReportDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}