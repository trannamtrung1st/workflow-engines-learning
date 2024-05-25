using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;

static class PredefinedBlocks
{
    public static readonly FunctionBlock Multiply = CreateBlockMultiply();
    public static readonly FunctionBlock Add = CreateBlockAdd();

    private static FunctionBlock CreateBlockMultiply()
    {
        var bMultiply = new FunctionBlock(id: "Multiply", name: "Multiply X * Y");

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
        bMultiply.DefaultTriggerEvent = eRun.Name;

        var outputEvents = new List<BlockEvent>();
        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        outputEvents.Add(eCompleted);
        bMultiply.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            await FB.Var(""{oResult.Name}"").Write({iX.Name} * {iY.Name});
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: @$"
            FB.LogWarning($""Invalid arguments {iX.Name}={{{iX.Name}}}, {iY.Name}={{{iY.Name}}}"");
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var logics = new[] { lRun, lHandleInvalid };
        bMultiply.Logics = logics;

        {
            var execControl = new BlockExecutionControlChart();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sInvalid = new BlockState("Invalid");
            var states = new[] { sIdle, sRunning, sInvalid };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Invalid = new BlockStateTransition(fromState: sIdle.Name, toState: sInvalid.Name, triggerEventName: eRun.Name);
                tIdle2Invalid.TriggerCondition = new(
                    id: "InvalidCondition",
                    name: "Invalid condition",
                    content: @$"return !FB.Var(""{iX.Name}"").Exists || !FB.Var(""{iY.Name}"").Exists;",
                    runtime: new CSharpScriptRuntime(usings: null));
                tIdle2Invalid.ActionLogicId = lHandleInvalid.Id;

                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicId = lRun.Id;

                transitions.Add(tIdle2Invalid);
                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name));
                transitions.Add(new(fromState: sInvalid.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bMultiply.ExecutionControlChart = execControl;
        }

        return bMultiply;
    }

    private static FunctionBlock CreateBlockAdd()
    {
        var bAdd = new FunctionBlock(id: "Add", name: "Add X + Y");

        var iX = new Variable("X", EDataType.Numeric);
        var iY = new Variable("Y", EDataType.Numeric);
        var inputs = new[] { iX, iY };
        bAdd.Inputs = inputs;

        var oResult = new Variable("Result", EDataType.Numeric);
        var outputs = new[] { oResult };
        bAdd.Outputs = outputs;

        var inputEvents = new List<BlockEvent>();
        var eRun = new BlockEvent(name: "Run", variableNames: new[] { iX.Name, iY.Name });
        inputEvents.Add(eRun);
        bAdd.InputEvents = inputEvents;
        bAdd.DefaultTriggerEvent = eRun.Name;

        var outputEvents = new List<BlockEvent>();
        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        outputEvents.Add(eCompleted);
        bAdd.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            await FB.Var(""{oResult.Name}"").Write({iX.Name} + {iY.Name});
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: @$"
            FB.LogWarning($""Invalid arguments {iX.Name}={{{iX.Name}}}, {iY.Name}={{{iY.Name}}}"");
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: new CSharpScriptRuntime(usings: null));
        var logics = new[] { lRun, lHandleInvalid };
        bAdd.Logics = logics;

        {
            var execControl = new BlockExecutionControlChart();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sInvalid = new BlockState("Invalid");
            var states = new[] { sIdle, sRunning, sInvalid };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Invalid = new BlockStateTransition(fromState: sIdle.Name, toState: sInvalid.Name, triggerEventName: eRun.Name);
                tIdle2Invalid.TriggerCondition = new(
                    id: "InvalidCondition",
                    name: "Invalid condition",
                    content: @$"return !FB.Var(""{iX.Name}"").Exists || !FB.Var(""{iY.Name}"").Exists;",
                    runtime: new CSharpScriptRuntime(usings: null));
                tIdle2Invalid.ActionLogicId = lHandleInvalid.Id;

                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicId = lRun.Id;

                transitions.Add(tIdle2Invalid);
                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name));
                transitions.Add(new(fromState: sInvalid.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bAdd.ExecutionControlChart = execControl;
        }

        return bAdd;
    }

}