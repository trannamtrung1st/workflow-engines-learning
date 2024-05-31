using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectanglePerimeterProcess
{
    public static FunctionBlockProcess Build(FunctionBlockInstance bAdd, FunctionBlockInstance bMultiply)
    {
        var process = new FunctionBlockProcess(id: "RectanglePerimeter", name: "Calculate perimeter of rectangle");
        var bInputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateInputBlock(
            new Variable(name: "Length", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Width", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "MulY", dataType: EDataType.Int, variableType: EVariableType.Output, defaultValue: 2)
        ), id: "Inputs");

        var bOutputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateOutputBlock(
            new Variable(name: "Result", dataType: EDataType.Numeric, variableType: EVariableType.Input)
        ), id: "Outputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd, bMultiply, bInputs, bOutputs };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bAdd.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bAdd.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bMultiply.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bMultiply.Id,
                SourceEventName = "Completed"
            });
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: "Length")
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Length"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: "Width")
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Width"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: null)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: null)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "MulY"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null)
            {
                SourceBlockId = bMultiply.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }
}