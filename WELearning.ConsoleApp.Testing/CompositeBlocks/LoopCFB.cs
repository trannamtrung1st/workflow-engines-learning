using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class LoopCFB
{
    public static CompositeBlockDef Build()
    {
        var cfb = new CompositeBlockDef(id: "Loop", name: "Sample loop CFB");

        var iN = new Variable("N", dataType: EDataType.Int, variableType: EVariableType.Input);
        var oResult = new Variable("Result", dataType: EDataType.Int, variableType: EVariableType.Output);
        cfb.Variables = new Variable[] { iN, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iN.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bAddDef = PredefinedBFBs.AddCsScript;
        var bAdd = new BlockInstance(definitionId: bAddDef.Id);
        var bLoopControllerDef = CreateLoopControllerBlock();
        var bLoopController = new BlockInstance(definitionId: bLoopControllerDef.Id);

        var bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "N", dataType: EDataType.Int, variableType: EVariableType.InOut),
            new Variable(name: "Inc", dataType: EDataType.Int, variableType: EVariableType.InOut, defaultValue: 1)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        var bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Result", dataType: EDataType.Int, variableType: EVariableType.InOut)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bAdd, bLoopController, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bLoopController.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bLoopController.Id, eventName: "Loop")
            {
                SourceBlockId = bAdd.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd.Id, eventName: "Trigger")
            {
                SourceBlockId = bLoopController.Id,
                SourceEventName = "Loop"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bLoopController.Id,
                SourceEventName = "Completed"
            });

            // [NOTE] CFB output events
            eventConnections.Add(new(blockId: null, eventName: "Completed")
            {
                SourceBlockId = bOutputs.Id,
                SourceEventName = "Completed"
            });

            cfb.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockDataConnection>();

            // [NOTE] CFB input data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Input || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: bInputs.Id, variableName: variable.Name, displayName: null, bindingType: EBindingType.Input)
                {
                    SourceVariableName = variable.Name
                });
            }

            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "N", displayName: "Loop count", bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "N"
            });
            dataConnections.Add(new(blockId: bLoopController.Id, variableName: "Result", displayName: "Loop result", bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bLoopController.Id,
                SourceVariableName = "Result"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Inc"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "Result", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bLoopController.Id,
                SourceVariableName = "Result"
            });
            cfb.DataConnections = dataConnections;
        }

        cfb.MapDefinitions(new[] { bAddDef, bLoopControllerDef, bInputsDef, bOutputsDef });
        return cfb;
    }

    static BasicBlockDef CreateLoopControllerBlock()
    {
        var assemblies = new[] { typeof(AppFramework).Assembly.FullName };
        var bLoopController = new BasicBlockDef(id: $"{nameof(LoopCFB)}_LoopController", name: "Loop controller");

        var iN = new Variable("N", EDataType.Int, EVariableType.Input);
        var ioResult = new Variable("Result", EDataType.Numeric, EVariableType.InOut);
        bLoopController.Variables = new[] { iN, ioResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iN.Name });
        var eiLoop = new BlockEvent(isInput: true, name: "Loop", variableNames: new[] { iN.Name, ioResult.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { ioResult.Name });
        var eoLoop = new BlockEvent(isInput: false, name: "Loop", variableNames: new[] { ioResult.Name });
        var events = new[] { eTrigger, eiLoop, eCompleted, eoLoop };
        bLoopController.Events = events;
        bLoopController.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Trigger",
            name: "Trigger",
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
        var fLoop = new Function(
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
        var functions = new[] { fRun, fLoop };
        bLoopController.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sLooping = new BlockState("Looping");
            var states = new[] { sIdle, sRunning, sLooping };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };

                var tRunning2Looping = new BlockStateTransition(fromState: sRunning.Name, toState: sLooping.Name, triggerEventName: eiLoop.Name);
                tRunning2Looping.ActionFunctionIds = new[] { fLoop.Id };

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name, triggerEventName: eiLoop.Name);
                tLooping2Looping.ActionFunctionIds = new[] { fLoop.Id };

                transitions.Add(tIdle2Running);
                transitions.Add(tRunning2Looping);
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