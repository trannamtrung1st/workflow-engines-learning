using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class RectangleAreaProcess
{
    public static FunctionBlockProcess Build(FunctionBlock bMultiply)
    {
        var process = new FunctionBlockProcess(id: "RectangleArea", name: "Calculate area of rectangle");

        {
            var blocks = new List<FunctionBlock> { bMultiply };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bMultiply.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bMultiply.Id, eventName: "Run", source: EEventSource.External));
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "X", displayName: "Length", source: EDataSource.External));
            dataConnections.Add(new(blockId: bMultiply.Id, variableName: "Y", displayName: "Width", source: EDataSource.External));
            process.DataConnections = dataConnections;
        }

        return process;
    }
}