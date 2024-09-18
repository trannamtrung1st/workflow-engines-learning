using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.Samples.DeviceService.FunctionBlock;

public static class PredefinedBFBs
{
    public static readonly BasicBlockDef MultiplyJs;
    public static readonly BasicBlockDef AddJs;

    static PredefinedBFBs()
    {
        MultiplyJs = CreateBlockMultiplyJs();
        AddJs = CreateBlockAddJs();
    }

    #region Multiply
    private static BasicBlockDef CreateBlockMultiplyJs()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.Javascript,
            multiplyScript: @$"
            Result = X * Y;
            EVENTS.Publish('Completed');
            ",
            handleInvalidScript: @$"
            console.trace(""Invalid arguments X, Y"");
            EVENTS.Publish('Completed');
            ",
            invalidConditionScript: @$"
            const x = FB.In(""X""); const y = FB.In(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: null
        );
    }

    private static BasicBlockDef CreateBlockMultiply(
        ERuntime runtime,
        string multiplyScript,
        string handleInvalidScript,
        string invalidConditionScript,
        IEnumerable<string> imports,
        IEnumerable<string> assemblies)
    {
        var bMultiply = new BasicBlockDef(id: $"Multiply{runtime}", name: $"Multiply X * Y ({runtime})");

        var iX = new Variable("X", EDataType.Numeric, EVariableType.Input);
        var iY = new Variable("Y", EDataType.Numeric, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bMultiply.Variables = new[] { iX, iY, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iX.Name, iY.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bMultiply.Events = new[] { eTrigger, eCompleted };
        bMultiply.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: multiplyScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var fHandleInvalid = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Handle invalid",
            content: handleInvalidScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var functions = new[] { fRun, fHandleInvalid };
        bMultiply.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sInvalid = new BlockState("Invalid");
            var states = new[] { sIdle, sRunning, sInvalid };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Invalid = new BlockStateTransition(fromState: sIdle.Name, toState: sInvalid.Name, triggerEventName: eTrigger.Name);
                tIdle2Invalid.TriggerCondition = new(
                    id: Guid.NewGuid().ToString(),
                    name: "Invalid condition",
                    content: invalidConditionScript,
                    runtime: runtime,
                    imports: imports, assemblies: assemblies, types: null);
                tIdle2Invalid.ActionFunctionIds = new[] { fHandleInvalid.Id };

                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };

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
    #endregion

    #region Add
    private static BasicBlockDef CreateBlockAddJs()
    {
        return CreateBlockAdd(
            runtime: ERuntime.Javascript,
            addScript: @$"
            Result = X + Y
            ",
            handleInvalidScript: @$"
            throw new Error('Invalid arguments');
            ",
            invalidConditionScript: @$"
            const x = FB.In(""X""); const y = FB.In(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: null
        );
    }

    private static BasicBlockDef CreateBlockAdd(ERuntime runtime,
        string addScript, string handleInvalidScript, string invalidConditionScript,
        IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bAdd = new BasicBlockDef(id: $"Add{runtime}", name: $"Add X + Y ({runtime})");

        var iX = new Variable("X", EDataType.Numeric, EVariableType.Input);
        var iY = new Variable("Y", EDataType.Numeric, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bAdd.Variables = new[] { iX, iY, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iX.Name, iY.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bAdd.Events = new[] { eTrigger, eCompleted };
        bAdd.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: addScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null,
            signature: "Add2Numbers", exported: true);
        var fHandleInvalid = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Handle invalid",
            content: handleInvalidScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var functions = new[] { fRun, fHandleInvalid };
        bAdd.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sInvalid = new BlockState("Invalid");
            var states = new[] { sIdle, sRunning, sInvalid };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Invalid = new BlockStateTransition(fromState: sIdle.Name, toState: sInvalid.Name, triggerEventName: eTrigger.Name);
                tIdle2Invalid.TriggerCondition = new(
                    id: Guid.NewGuid().ToString(),
                    name: "Invalid condition",
                    content: invalidConditionScript,
                    runtime: runtime,
                    imports: imports, assemblies: assemblies, types: null);
                tIdle2Invalid.ActionFunctionIds = new[] { fHandleInvalid.Id };

                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };
                tIdle2Running.DefaultOutputEvents = new[] { eCompleted.Name };

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
    #endregion
}