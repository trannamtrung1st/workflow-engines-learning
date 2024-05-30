using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.Processes;

public static class LoopProcess
{
    public static FunctionBlockProcess Build()
    {
        var process = new FunctionBlockProcess(id: "Loop", name: "Sample loop process");

        var bAdd = new FunctionBlockInstance(definition: PredefinedBlocks.AddCsScript);
        var bLoopController = new FunctionBlockInstance(definition: CreateLoopControllerBlock());
        var bInputs = new FunctionBlockInstance(definition: PredefinedBlocks.CreateInOutBlock(
            new Variable(name: "One", dataType: EDataType.Int, bindingType: EBindingType.InOut, defaultValue: 1)
        ), id: "Inputs");

        {
            var blocks = new List<FunctionBlockInstance> { bAdd, bLoopController, bInputs };
            process.Blocks = blocks;
            process.DefaultBlockIds = new[] { bLoopController.Id };
        }

        {
            var eventConnections = new List<BlockEventConnection>();
            eventConnections.Add(new(blockId: bLoopController.Id, eventName: "Run", source: EEventSource.External));
            eventConnections.Add(new(blockId: bLoopController.Id, eventName: "Loop", source: EEventSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd.Id, eventName: "Run", source: EEventSource.Internal)
            {
                SourceBlockId = bLoopController.Id,
                SourceEventName = "Loop"
            });
            process.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();
            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "N", displayName: "Loop count", variableType: EBindingType.Input, source: EDataSource.External));
            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "Result", displayName: "Loop result", variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bLoopController.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: null, variableType: EBindingType.Input, source: EDataSource.Internal)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "One"
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }

    static FunctionBlock CreateLoopControllerBlock()
    {
        var assemblies = new[] { typeof(AppFramework).Assembly };
        var bLoopController = new FunctionBlock(id: "LoopController", name: "Loop controller");

        var iN = new Variable("N", EDataType.Int, EBindingType.Input);
        var ioResult = new Variable("Result", EDataType.Numeric, EBindingType.InOut);
        bLoopController.Variables = new[] { iN, ioResult };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: new[] { iN.Name });
        var eiLoop = new BlockEvent(isInput: true, name: "Loop", variableNames: new[] { iN.Name, ioResult.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { ioResult.Name });
        var eoLoop = new BlockEvent(isInput: false, name: "Loop", variableNames: new[] { ioResult.Name });
        var events = new[] { eRun, eiLoop, eCompleted, eoLoop };
        bLoopController.Events = events;
        bLoopController.DefaultTriggerEvent = eRun.Name;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            await FB.Out(""Result"").Set(0.0);
            var n = FB.In(""N"").ToInt();
            if (n > 0) 
                await FB.Publish(""Loop"");
            else 
                await FB.Publish(""Completed"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var lLoop = new Logic(
            id: "Loop",
            name: "Loop",
            content: @$"
            var currentResult = FB.InOut(""Result"").ToInt();
            var n = FB.In(""N"").ToInt();
            if (currentResult < n) 
                await FB.Publish(""Loop"");
            else 
                await FB.Publish(""Completed"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var logics = new[] { lRun, lLoop };
        bLoopController.Logics = logics;

        {
            var execControl = new BlockExecutionControlChart();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sLooping = new BlockState("Looping");
            var states = new[] { sIdle, sRunning, sLooping };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicIds = new[] { lRun.Id };

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name, triggerEventName: eiLoop.Name);
                tLooping2Looping.ActionLogicIds = new[] { lLoop.Id };

                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sLooping.Name)
                {
                    TriggerCondition = new(
                        id: "Running2LoopingCondition",
                        name: "Running to looping condition",
                        content: @$"
                            var n = FB.In(""N"").ToInt();
                            return n > 0;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies, types: null
                    )
                });
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name)
                {
                    TriggerCondition = new(
                        id: "Running2IdleCondition",
                        name: "Running to idle condition",
                        content: @$"
                            var n = FB.In(""N"").ToInt();
                            return n <= 0;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies, types: null
                    )
                });
                transitions.Add(tLooping2Looping);
                transitions.Add(new(fromState: sLooping.Name, toState: sIdle.Name)
                {
                    TriggerCondition = new(
                        id: "Looping2IdleCondition",
                        name: "Looping to idle condition",
                        content: @$"
                            var n = FB.In(""N"").ToInt();
                            var result = FB.In(""Result"").ToDouble();
                            return result >= n;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies, types: null
                    )
                });
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bLoopController.ExecutionControlChart = execControl;
        }

        return bLoopController;
    }
}