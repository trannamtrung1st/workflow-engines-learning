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

        var bAdd = new FunctionBlockInstance(PredefinedBlocks.AddCsScript);
        var bLoopController = new FunctionBlockInstance(definition: CreateLoopControllerBlock());

        {
            var blocks = new List<FunctionBlockInstance> { bAdd, bLoopController };
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
            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "N", displayName: "Loop count", source: EDataSource.External));
            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "CurrentResult", displayName: "Current result", source: EDataSource.Internal)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: null, source: EDataSource.Internal)
            {
                SourceBlockId = bLoopController.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: null, source: EDataSource.Internal)
            {
                ConstantValue = 1
            });
            process.DataConnections = dataConnections;
        }

        return process;
    }

    static FunctionBlock CreateLoopControllerBlock()
    {
        var assemblies = new[] { typeof(AppFramework).Assembly };
        var bLoopController = new FunctionBlock(id: "LoopController", name: "Loop controller");

        var iN = new Variable("N", EDataType.Int);
        var iCurrentResult = new Variable("CurrentResult", EDataType.Numeric);
        bLoopController.Inputs = new[] { iN, iCurrentResult };

        var oResult = new Variable("Result", EDataType.Numeric);
        var outputs = new[] { oResult };
        bLoopController.Outputs = outputs;

        var eRun = new BlockEvent(name: "Run", variableNames: new[] { iN.Name });
        var eiLoop = new BlockEvent(name: "Loop", variableNames: new[] { iN.Name, iCurrentResult.Name });
        var inputEvents = new[] { eRun, eiLoop };
        bLoopController.InputEvents = inputEvents;
        bLoopController.DefaultTriggerEvent = eRun.Name;

        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        var eoLoop = new BlockEvent(name: "Loop", variableNames: new[] { oResult.Name });
        var outputEvents = new[] { eCompleted, eoLoop };
        bLoopController.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            await FB.Set(""{oResult.Name}"", 0.0);
            var n = (int)FB.Get(""{iN.Name}"").Value;
            if (n > 0) 
                await FB.Publish(""{eoLoop.Name}"");
            else 
                await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
        var lLoop = new Logic(
            id: "Loop",
            name: "Loop",
            content: @$"
            var currentResult = (double)FB.Get(""{iCurrentResult.Name}"").Value;
            FB.Set(""{oResult.Name}"", currentResult);
            var n = (int)FB.Get(""{iN.Name}"").Value;
            if (currentResult < n) 
                await FB.Publish(""{eoLoop.Name}"");
            else 
                await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
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
                tIdle2Running.ActionLogicId = lRun.Id;

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name, triggerEventName: eiLoop.Name);
                tLooping2Looping.ActionLogicId = lLoop.Id;

                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sLooping.Name)
                {
                    TriggerCondition = new(
                        id: "Running2LoopingCondition",
                        name: "Running to looping condition",
                        content: @$"
                            var n = (int)FB.Get(""{iN.Name}"").Value;
                            return n > 0;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies
                    )
                });
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name)
                {
                    TriggerCondition = new(
                        id: "Running2IdleCondition",
                        name: "Running to idle condition",
                        content: @$"
                            var n = (int)FB.Get(""{iN.Name}"").Value;
                            return n <= 0;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies
                    )
                });
                transitions.Add(tLooping2Looping);
                transitions.Add(new(fromState: sLooping.Name, toState: sIdle.Name)
                {
                    TriggerCondition = new(
                        id: "Looping2IdleCondition",
                        name: "Looping to idle condition",
                        content: @$"
                            var n = (int)FB.Get(""{iN.Name}"").Value;
                            var result = (double)FB.Get(""{oResult.Name}"").Value;
                            return result >= n;
                        ",
                        runtime: ERuntime.CSharpScript,
                        imports: null, assemblies: assemblies
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