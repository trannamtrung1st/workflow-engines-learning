using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Helpers;
using WELearning.DynamicCodeExecution.Constants;

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

        var bAddDef = PredefinedBFBs.AddJs;
        var bRandomDef = PredefinedBFBs.RandomJs;
        var bCustomAddDef = BlockHelper.CreateBlockSimple(
            id: "CustomAdd",
            name: "A custom add that reuses functions from other BFBs",
            content:
@$"const addResult = Add2Numbers({{ X: Input.X, Y: Input.Y }}, {{ Result: null }});
const randomResult = Random(null, {{ Result: null }});
Result = addResult.Result + randomResult.Result;",
            runtime: ERuntime.Javascript, imports: new[] { $"import {{ Add2Numbers, Random }} from '{FunctionDefaults.ModuleFunctions}'" },
            importModuleRefs: [new(
                Id: Guid.NewGuid().ToString(),
                ModuleName: FunctionDefaults.ModuleFunctions,
                BlockIds: [bAddDef.Id, bRandomDef.Id])],
            signature: null, exported: false,
            new Variable("Input", dataType: EDataType.Object, variableType: EVariableType.Input),
            new Variable("Result", dataType: EDataType.Numeric, variableType: EVariableType.Output)
        );
        var bCustomAdd = new BlockInstance(definitionId: bCustomAddDef.Id);

        var bInputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Input", dataType: EDataType.Object, variableType: EVariableType.InOut)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Output", dataType: EDataType.String, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bCustomAdd, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bCustomAdd.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bCustomAdd.Id,
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

            // [NOTE] CFB input data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Input || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: bInputs.Id, variableName: variable.Name, displayName: null, bindingType: EBindingType.Input)
                {
                    SourceVariableName = variable.Name
                });
            }

            dataConnections.Add(new(blockId: bCustomAdd.Id, variableName: "Input", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Input"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Output", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bCustomAdd.Id,
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

        {
            var references = new List<BlockConnection>();
            cfb.References = references;
        }

        cfb.MapDefinitions(new[] { bAddDef, bRandomDef, bCustomAddDef, bInputsDef, bOutputsDef });
        return cfb;
    }

}