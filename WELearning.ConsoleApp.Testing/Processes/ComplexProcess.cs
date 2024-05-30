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
        var bAdd1 = new FunctionBlockInstance(bAddDef, id: "Add1");
        var bMul = new FunctionBlockInstance(bMultiplyDef, id: "Mul");
        var bDelay = new FunctionBlockInstance(bDelayDef, id: "Delay");
        var bRandom = new FunctionBlockInstance(bRandomDef, id: "Random");
        var bAdd2 = new FunctionBlockInstance(bAddDef, id: "Add2");
        var bInputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateInOutBlock(
            new Variable(name: "MulY", dataType: EDataType.Int, bindingType: EBindingType.InOut, defaultValue: 2),
            new Variable(name: "DelayMs", dataType: EDataType.Int, bindingType: EBindingType.InOut, defaultValue: 10)
        ), id: "Inputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd1, bMul, bDelay, bRandom, bAdd2, bInputs };
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
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bMul.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMul.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "MulY"
            });
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "DelayMs"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bMul.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bRandom.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }
}