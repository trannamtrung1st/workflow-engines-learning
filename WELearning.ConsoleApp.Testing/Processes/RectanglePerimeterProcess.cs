using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectanglePerimeterProcess
{
    public static FunctionBlockProcess Build()
    {
        var process = new FunctionBlockProcess(id: "RectanglePerimeter", name: "Calculate perimeter of rectangle");

        var bAdd = PredefinedBlocks.Add;
        var bMultiply = PredefinedBlocks.Multiply;
        {
            var blocks = new List<FunctionBlock> { bAdd, bMultiply };
            process.Blocks = blocks;
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
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", source: EDataSource.External));
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", source: EDataSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", source: EDataSource.Internal)
            {
                ConstantVariable = new(name: "2", dataType: EDataType.Int, constantValue: 2)
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }
}