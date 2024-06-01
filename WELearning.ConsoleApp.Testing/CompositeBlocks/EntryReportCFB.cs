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

        var i1 = new Variable("Input1", dataType: EDataType.Reference, variableType: EVariableType.Input);
        var i2 = new Variable("Input2", dataType: EDataType.Reference, variableType: EVariableType.Input);
        var oResult = new Variable("Result", dataType: EDataType.Reference, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { i1, i2, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { i1.Name, i2.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bConcatDef = PredefinedBFBs.ConcatTwoStringsJs;
        var bConcat = new BlockInstance(bConcatDef.Id);

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Input1", dataType: EDataType.Reference, variableType: EVariableType.InOut),
            new Variable(name: "Input2", dataType: EDataType.Reference, variableType: EVariableType.InOut),
            new Variable(name: "Delimiter", dataType: EDataType.String, variableType: EVariableType.InOut, defaultValue: " ")
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var entryType = nameof(EntryEntity);
        var bOutputsDef = PredefinedBFBs.CreatePassThroughBlock(passThroughVars: ("Result", entryType));
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bConcat, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bConcat.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bConcat.Id,
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

            // [NOTE] CFB input data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Input || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: bInputs.Id, variableName: variable.Name, displayName: null, bindingType: EBindingType.Input)
                {
                    SourceVariableName = variable.Name
                });
            }

            dataConnections.Add(new(blockId: bConcat.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Input1"
            });
            dataConnections.Add(new(blockId: bConcat.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Input2"
            });
            dataConnections.Add(new(blockId: bConcat.Id, variableName: "Delimiter", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Delimiter"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bConcat.Id,
                SourceVariableName = "Result"
            });

            // [NOTE] CFB output data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Output || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: bOutputs.Id, variableName: variable.Name, displayName: null, bindingType: EBindingType.Output)
                {
                    SourceVariableName = variable.Name
                });
            }

            cfb.DataConnections = dataConnections;
        }

        cfb.MapDefinitions(new[] { bConcatDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}