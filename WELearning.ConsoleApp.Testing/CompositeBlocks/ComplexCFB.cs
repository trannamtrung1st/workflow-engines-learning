using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class ComplexCFB
{
    public static CompositeBlockDef Build(
        BasicBlockDef bAddDef, BasicBlockDef bMultiplyDef,
        BasicBlockDef bRandomDef, BasicBlockDef bDelayDef)
    {
        var cfb = new CompositeBlockDef(id: "Complex", name: "A complex CFB");

        var iAdd1X = new Variable("Add1X", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var iAdd1Y = new Variable("Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.Input);
        var oResult = new Variable("Result", dataType: EDataType.Numeric, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iAdd1X, iAdd1Y, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iAdd1X.Name, iAdd1Y.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bAdd1 = new BlockInstance(bAddDef.Id, id: "Add1");
        var bMul = new BlockInstance(bMultiplyDef.Id, id: "Mul");
        var bDelay = new BlockInstance(bDelayDef.Id, id: "Delay");
        var bRandom = new BlockInstance(bRandomDef.Id, id: "Random");
        var bAdd2 = new BlockInstance(bAddDef.Id, id: "Add2");

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Add1X", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.InOut),
            new Variable(name: "MulY", dataType: EDataType.Int, variableType: EVariableType.InOut, defaultValue: 2),
            new Variable(name: "DelayMs", dataType: EDataType.Int, variableType: EVariableType.InOut, defaultValue: 10)
        );
        var bInputs = new BlockInstance(bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Result", dataType: EDataType.Int, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bAdd1, bMul, bDelay, bRandom, bAdd2, bInputs, bOutputs };
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
            eventConnections.Add(new(blockId: bMul.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bDelay.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bRandom.Id, eventName: "Trigger")
            {
                SourceBlockId = bDelay.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd2.Id, eventName: "Trigger")
            {
                SourceBlockId = bMul.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd2.Id,
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
            dataConnections.Add(new(blockId: bMul.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMul.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "MulY"
            });
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "DelayMs"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMul.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bRandom.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd2.Id,
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

        cfb.MapDefinitions(new[] { bAddDef, bMultiplyDef, bRandomDef, bDelayDef, bInputsDef, bOutputsDef });
        return cfb;
    }
}