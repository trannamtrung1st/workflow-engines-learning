using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class RectangleAreaCFB
{
    public static CompositeBlockDef Build(BasicBlockDef bMultiplyDef)
    {
        var cfb = new CompositeBlockDef(id: "RectangleArea", name: "Calculate area of rectangle");

        var iLength = new Variable("Length", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iWidth = new Variable("Width", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var oResult = new Variable("Result", dataType: EDataType.Numeric, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iLength, iWidth, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iLength.Name, iWidth.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bMultiply = new BlockInstance(bMultiplyDef.Id);

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Length", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Width", dataType: EDataType.Numeric, variableType: EVariableType.InOut)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Result", dataType: EDataType.Numeric, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bMultiply, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bMultiply.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bMultiply.Id,
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

            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: "Length", bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Length"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: "Width", bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Width"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMultiply.Id,
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

        cfb.MapDefinitions(new[] { bMultiplyDef, bInputsDef, bOutputsDef });
        return cfb;
    }
}