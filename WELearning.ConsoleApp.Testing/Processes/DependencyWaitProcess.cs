using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class DependencyWaitProcess
{
    public static FunctionBlockProcess Build()
    {
        var process = new FunctionBlockProcess(id: "DependencyWait", name: "Sample dependency wait process");

        var bAdd1 = new FunctionBlockInstance(PredefinedBlocks.AddJs.Id, id: "Add1", displayName: "Add 1");
        var bAdd2 = new FunctionBlockInstance(PredefinedBlocks.AddCsScript.Id, id: "Add2", displayName: "Add 2");
        var bAdd3 = new FunctionBlockInstance(PredefinedBlocks.AddJs.Id, id: "Add3", displayName: "Add 3");
        var bDelay = new FunctionBlockInstance(PredefinedBlocks.DelayCsScript.Id);
        var bInputsDef = PredefinedBlocks.CreateInputBlock(
            new Variable(name: "DelayMs", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Add1X", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Add1Y", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Add2X", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Add2Y", dataType: EDataType.Numeric, variableType: EVariableType.Output)
        );
        var bInputs = new FunctionBlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBlocks.CreateOutputBlock(
            new Variable(name: "Result", dataType: EDataType.Numeric, variableType: EVariableType.Input)
        );
        var bOutputs = new FunctionBlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd1, bAdd2, bAdd3, bDelay, bInputs, bOutputs };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bAdd1.Id, bDelay.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bAdd1.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bDelay.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bAdd2.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bDelay.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd3.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd3.Id,
                SourceEventName = "Completed"
            });
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: "Delay ms")
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "DelayMs"
            });
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
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add2X"
            });
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Add2Y"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bAdd2.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null)
            {
                SourceBlockId = bAdd3.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        process.MapDefinitions(new[] { PredefinedBlocks.AddJs, PredefinedBlocks.AddCsScript, PredefinedBlocks.DelayCsScript, bInputsDef, bOutputsDef });
        return process;
    }

}