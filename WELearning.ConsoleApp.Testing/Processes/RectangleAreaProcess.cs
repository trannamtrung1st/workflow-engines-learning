using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectangleAreaProcess
{
    public static FunctionBlockProcess Build(FunctionBlockInstance bMultiply)
    {
        var process = new FunctionBlockProcess(id: "RectangleArea", name: "Calculate area of rectangle");
        var bInputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateInputBlock(
            new Variable(name: "Length", dataType: EDataType.Numeric, variableType: EVariableType.Output),
            new Variable(name: "Width", dataType: EDataType.Numeric, variableType: EVariableType.Output)
        ), id: "Inputs");

        var bOutputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateOutputBlock(
            new Variable(name: "Result", dataType: EDataType.Numeric, variableType: EVariableType.Input)
        ), id: "Outputs");

        {
            var blocks = new List<FunctionBlockInstance> { bMultiply, bInputs, bOutputs };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bMultiply.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bMultiply.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bMultiply.Id,
                SourceEventName = "Completed"
            });
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: "Length")
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Length"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: "Width")
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Width"
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