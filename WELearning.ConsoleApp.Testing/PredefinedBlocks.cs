using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Shared.Constants;

static class PredefinedBlocks
{
    public static readonly FunctionBlock Multiply = CreateBlockMultiply();

    private static FunctionBlock CreateBlockMultiply()
    {
        var bMultiply = new FunctionBlock(id: "Multiply", name: "Multiply");

        var iX = new Variable("X", EDataType.Numeric);
        var iY = new Variable("Y", EDataType.Numeric);
        var inputs = new[] { iX, iY };
        bMultiply.Inputs = inputs;

        var oResult = new Variable("Result", EDataType.Numeric);
        var outputs = new[] { oResult };
        bMultiply.Outputs = outputs;

        var inputEvents = new List<BlockEvent>();
        var eRun = new BlockEvent(name: "Run", variableNames: new[] { iX.Name, iY.Name });
        inputEvents.Add(eRun);
        bMultiply.InputEvents = inputEvents;

        var outputEvents = new List<BlockEvent>();
        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        outputEvents.Add(eCompleted);
        bMultiply.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "RunCs",
            name: "Run (C#)",
            content: @$"
            await FB.Write(""{oResult.Name}"", X * Y);
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var lHandleInvalid = new Logic(
            id: "HandleInvalidCs",
            name: "Handle invalid (C#)",
            content: @$"
            FB.LogWarning($""Invalid arguments X={{X}}, Y={{Y}}"");
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var logics = new[] { lRun, lHandleInvalid };
        bMultiply.Logics = logics;

        {
            var execControl = new BlockExecutionControl();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sInvalid = new BlockState("Invalid");
            var states = new[] { sIdle, sRunning, sInvalid };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Invalid = new BlockStateTransition(fromState: sIdle.Name, toState: sInvalid.Name, triggerEventName: eRun.Name);
                var tIdle2InvalidConditions = new List<Logic>();
                tIdle2InvalidConditions.Add(new(
                    id: "InvalidConditionCs",
                    name: "Invalid condition (C#)",
                    content: "return Length <= 0 || Width <= 0;",
                    runtime: new CSharpScriptRuntime(usings: null)));
                tIdle2Invalid.TriggerConditions = tIdle2InvalidConditions;
                tIdle2Invalid.ActionLogicIds = new[] { lHandleInvalid.Id };

                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicIds = new[] { lRun.Id };

                transitions.Add(tIdle2Invalid);
                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name));
                transitions.Add(new(fromState: sInvalid.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle;
            bMultiply.ExecutionControl = execControl;
        }

        return bMultiply;
    }
}