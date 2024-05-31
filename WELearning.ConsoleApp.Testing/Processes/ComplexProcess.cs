using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class ComplexProcess
{
    public static FunctionBlockProcess Build(
        FunctionBlock bAddDef, FunctionBlock bMultiplyDef,
        FunctionBlock bRandomDef, FunctionBlock bDelayDef)
    {
        var process = new FunctionBlockProcess(id: "Complex", name: "A complex process");
        var bAdd1 = new FunctionBlockInstance(bAddDef.Id, id: "Add1");
        var bMul = new FunctionBlockInstance(bMultiplyDef.Id, id: "Mul");
        var bDelay = new FunctionBlockInstance(bDelayDef.Id, id: "Delay");
        var bRandom = new FunctionBlockInstance(bRandomDef.Id, id: "Random");
        var bAdd2 = new FunctionBlockInstance(bAddDef.Id, id: "Add2");

        var bInputsDef = PredefinedBlocks.CreateInputBlock(
            new Variable(name: "Add1X", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "MulY", dataType: EDataType.Int, variableType: EVariableType.Output, defaultValue: 2),
            new Variable(name: "DelayMs", dataType: EDataType.Int, variableType: EVariableType.Output, defaultValue: 10)
        );
        var bInputs = new FunctionBlockInstance(bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBlocks.CreateOutputBlock(
            new Variable(name: "Result", dataType: EDataType.Int, variableType: EVariableType.Input)
        );
        var bOutputs = new FunctionBlockInstance(bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd1, bMul, bDelay, bRandom, bAdd2, bInputs, bOutputs };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bAdd1.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bAdd1.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bMul.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bDelay.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bRandom.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bDelay.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd2.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bMul.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd2.Id,
                SourceEventName = "Completed"
            });
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add1X"
            });
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add1Y"
            });
            dataConnections.Add(new(blockId: bMul.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMul.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "MulY"
            });
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "DelayMs"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bMul.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bRandom.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null)
            {
                SourceBlockId = bAdd2.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        process.MapDefinitions(new[] { bAddDef, bMultiplyDef, bRandomDef, bDelayDef, bInputsDef, bOutputsDef });
        return process;
    }
}