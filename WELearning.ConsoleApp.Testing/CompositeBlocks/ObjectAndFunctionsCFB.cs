using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class ObjectAndFunctionsCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "ObjectAndFunctions", name: "Demo using object as payload and reuse functions in other BFBs");

        var iInput = new Variable("Input", dataType: EDataType.Any, variableType: EVariableType.Input);
        var oOutput = new Variable("Output", dataType: EDataType.String, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iInput, oOutput };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iInput.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oOutput.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bLogInputDef = PredefinedBFBs.LogInputJs;
        var bLogInput = new BlockInstance(definitionId: bLogInputDef.Id);

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Input", dataType: EDataType.Any, variableType: EVariableType.InOut)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Output", dataType: EDataType.String, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bLogInput, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bLogInput.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bLogInput.Id,
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

            dataConnections.Add(new(blockId: bLogInput.Id, variableName: "Data", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Input"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Output", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bLogInput.Id,
                SourceVariableName = "Data"
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
            var references = new List<BlockReference>();
            cfb.References = references;
        }

        cfb.MapDefinitions(new[] { bLogInputDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}