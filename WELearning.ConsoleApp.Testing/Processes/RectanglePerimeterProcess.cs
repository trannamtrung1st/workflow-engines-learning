using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectanglePerimeterProcess
{
    public static FunctionBlockProcess Build(FunctionBlockInstance bAdd, FunctionBlockInstance bMultiply)
    {
        var process = new FunctionBlockProcess(id: "RectanglePerimeter", name: "Calculate perimeter of rectangle");
        var bInputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateInOutBlock(
            new Variable(name: "MulY", dataType: EDataType.Int, bindingType: EBindingType.InOut, defaultValue: 2)
        ), id: "Inputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd, bMultiply, bInputs };
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
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: "Length", variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: "Width", variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "MulY"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }
}