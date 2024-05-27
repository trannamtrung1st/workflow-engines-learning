using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class DependencyWaitProcess
{
    public static FunctionBlockProcess Build()
    {
        var process = new FunctionBlockProcess(id: "DependencyWait", name: "Sample dependency wait process");

        var bAdd1 = new FunctionBlockInstance(PredefinedBlocks.AddJs, id: "Add1", displayName: "Add 1");
        var bAdd2 = new FunctionBlockInstance(PredefinedBlocks.AddCsScript, id: "Add2", displayName: "Add 2");
        var bAdd3 = new FunctionBlockInstance(PredefinedBlocks.AddJs, id: "Add3", displayName: "Add 3");
        var bDelay = new FunctionBlockInstance(definition: CreateBlockDelay());

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
            dataConnections.Add(new(blockId: bDelay.Id, variableName: "Ms", displayName: "Delay ms", source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "X", displayName: null, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd1.Id, variableName: "Y", displayName: null, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "X", displayName: null, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd2.Id, variableName: "Y", displayName: null, source: EDataSource.External));
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "X", displayName: null, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd1.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd3.Id, variableName: "Y", displayName: null, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd2.Id,
                SourceVariableName = "Result"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }

    private static FunctionBlock CreateBlockDelay()
    {
        var assemblies = new[] { typeof(AppFramework).Assembly };
        var bDelay = new FunctionBlock(id: "Delay", name: "Delay");

        var iMs = new Variable("Ms", EDataType.Int);
        var inputs = new[] { iMs };
        bDelay.Inputs = inputs;

        bDelay.Outputs = Array.Empty<Variable>();

        var inputEvents = new List<BlockEvent>();
        var eRun = new BlockEvent(name: "Run", variableNames: new[] { iMs.Name });
        inputEvents.Add(eRun);
        bDelay.InputEvents = inputEvents;
        bDelay.DefaultTriggerEvent = eRun.Name;

        var outputEvents = new List<BlockEvent>();
        var eCompleted = new BlockEvent(name: "Completed", variableNames: Array.Empty<string>());
        outputEvents.Add(eCompleted);
        bDelay.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            var ms = (int)FB.Get(""{iMs.Name}"").Value;
            await Task.Delay(ms);
            await FB.Publish(""{eCompleted.Name}"")
            ",
            runtime: ERuntime.CSharpScript,
            imports: new[] { "System.Threading.Tasks" }, assemblies: assemblies);
        var logics = new[] { lRun };
        bDelay.Logics = logics;

        {
            var execControl = new BlockExecutionControlChart();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var states = new[] { sIdle, sRunning };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicId = lRun.Id;

                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bDelay.ExecutionControlChart = execControl;
        }

        return bDelay;
    }

}