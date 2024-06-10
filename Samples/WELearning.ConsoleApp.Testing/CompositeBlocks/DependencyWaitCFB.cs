using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class DependencyWaitCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "DependencyWait", name: "Sample dependency wait CFB");

        var iDelayMs = new Variable("DelayMs", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iAdd1X = new Variable("Add1X", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iAdd1Y = new Variable("Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iAdd2X = new Variable("Add2X", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iAdd2Y = new Variable("Add2Y", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var oResult = new Variable("Result", dataType: EDataType.Numeric, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iDelayMs, iAdd1X, iAdd1Y, iAdd2X, iAdd2Y, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger",
            variableNames: new[] { iDelayMs.Name, iAdd1X.Name, iAdd1Y.Name, iAdd2X.Name, iAdd2Y.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bAdd1 = new BlockInstance(PredefinedBFBs.AddJs.Id, id: "Add1", displayName: "Add 1");
        var bAdd2 = new BlockInstance(PredefinedBFBs.AddCsScript.Id, id: "Add2", displayName: "Add 2");
        var bAdd3 = new BlockInstance(PredefinedBFBs.AddJs.Id, id: "Add3", displayName: "Add 3");
        var bDelay = new BlockInstance(PredefinedBFBs.DelayCsScript.Id);
        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "DelayMs", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Add1X", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Add2X", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Add2Y", dataType: EDataType.Numeric, variableType: EVariableType.InOut)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Result", dataType: EDataType.Numeric, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bAdd1, bAdd2, bAdd3, bDelay, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bAdd1.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bDelay.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd2.Id, eventName: "Trigger")
            {
                SourceBlockId = bDelay.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd3.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd3.Id,
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

            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: "Delay ms", bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "DelayMs"
            });
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add1X"
            });
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add1Y"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add2X"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add2Y"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd2.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd3.Id,
                SourceVariableName = "Result"
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

        cfb.MapDefinitions(new[] { PredefinedBFBs.AddJs, PredefinedBFBs.AddCsScript, PredefinedBFBs.DelayCsScript, bInputsDef, bOutputsDef });
        return cfb;
    }

}