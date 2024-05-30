using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class DependencyWaitProcess
{
    public static FunctionBlockProcess Build()
    {
        var process = new FunctionBlockProcess(id: "DependencyWait", name: "Sample dependency wait process");

        var bAdd1 = new FunctionBlockInstance(PredefinedBlocks.AddJs, id: "Add1", displayName: "Add 1");
        var bAdd2 = new FunctionBlockInstance(PredefinedBlocks.AddCsScript, id: "Add2", displayName: "Add 2");
        var bAdd3 = new FunctionBlockInstance(PredefinedBlocks.AddJs, id: "Add3", displayName: "Add 3");
        var bDelay = new FunctionBlockInstance(definition: PredefinedBlocks.DelayCsScript);

        {
            var blocks = new List<FunctionBlockInstance> { bAdd1, bAdd2, bAdd3, bDelay };
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
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: "Delay ms", variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd2.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }

}