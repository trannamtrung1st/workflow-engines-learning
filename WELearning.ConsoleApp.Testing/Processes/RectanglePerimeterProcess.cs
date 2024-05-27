using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectanglePerimeterProcess
{
    public static FunctionBlockProcess Build(FunctionBlockInstance bAdd, FunctionBlockInstance bMultiply)
    {
        var process = new FunctionBlockProcess(id: "RectanglePerimeter", name: "Calculate perimeter of rectangle");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd, bMultiply };
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
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: "Length", source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: "Width", source: EDataSource.External));
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: null, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: null, source: EDataSource.Internal)
            {
                ConstantValue = 2
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }
}