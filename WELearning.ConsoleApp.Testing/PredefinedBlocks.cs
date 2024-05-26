using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;

static class PredefinedBlocks
{
    public static readonly FunctionBlock MultiplyCsScript = CreateBlockMultiplyCsScript();
    public static readonly FunctionBlock MultiplyCsCompiled = CreateBlockMultiplyCsCompiled();
    public static readonly FunctionBlock Add = CreateBlockAdd();

    #region Multiply
    private static FunctionBlock CreateBlockMultiplyCsScript()
    {
        return CreateBlockMultiplyCs(
            multiplyScriptProvider: s => s,
            handleInvalidScriptProvider: s => s,
            invalidConditionScriptProvider: s => s,
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockMultiplyCsCompiled()
    {
        var mscorlib = typeof(object).GetTypeInfo().Assembly;
        var assemblies = new List<Assembly> { mscorlib };
        var dllNames = new string[]
        {
            "System.Runtime",
            // "System.Net.Http",
            // "System.Threading",
            // "System.Core",
            // "System.Linq",
            // "netstandard",
        };
        assemblies.AddRange(dllNames.Select(dll => Assembly.Load(dll)));

        return CreateBlockMultiplyCs(
            multiplyScriptProvider: s => BaseBlockFrameworkFunction.WrapScript(s),
            handleInvalidScriptProvider: s => BaseBlockFrameworkFunction.WrapScript(s),
            invalidConditionScriptProvider: s => BaseBlockFrameworkFunction<bool>.WrapScript(s),
            runtime: ERuntime.CSharpCompiled,
            imports: new[]
            {
                "System.Threading",
                "System.Threading.Tasks",
                "WELearning.Core.FunctionBlocks.Framework"
            }, assemblies: assemblies
        );
    }

    private static FunctionBlock CreateBlockMultiplyCs(
        Func<string, string> multiplyScriptProvider,
        Func<string, string> handleInvalidScriptProvider,
        Func<string, string> invalidConditionScriptProvider,
        ERuntime runtime,
        IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies)
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
            content: multiplyScriptProvider(@$"
            var x = FB.GetDouble(""{iX.Name}"");
            var y = FB.GetDouble(""{iY.Name}"");
            var result = x * y;
            await FB.Set(""{oResult.Name}"", result);
            await FB.Publish(""{eCompleted.Name}"");
            "),
            runtime: runtime,
            imports: imports, assemblies: assemblies);
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: handleInvalidScriptProvider(@$"
            FB.LogWarning(""Invalid arguments {iX.Name}, {iY.Name}"");
            await FB.Publish(""{eCompleted.Name}"");
            "),
            runtime: runtime,
            imports: imports, assemblies: assemblies);
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
                    content: invalidConditionScriptProvider(@$"
                        var x = FB.Get(""{iX.Name}""); var y = FB.Get(""{iY.Name}"");
                        return !x.Exists || !y.Exists || !x.IsNumeric || !y.IsNumeric;
                    "),
                    runtime: runtime,
                    imports: imports, assemblies: assemblies);
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
    #endregion

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
            var x = FB.GetDouble(""{iX.Name}"");
            var y = FB.GetDouble(""{iY.Name}"");
            var result = x + y;
            await FB.Set(""{oResult.Name}"", result);
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: null);
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: @$"
            FB.LogWarning(""Invalid arguments {iX.Name}, {iY.Name}"");
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: null);
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
                    content: @$"
                        var x = FB.Get(""{iX.Name}""); var y = FB.Get(""{iY.Name}"");
                        return !x.Exists || !y.Exists || !x.IsNumeric || !y.IsNumeric;
                    ",
                    runtime: ERuntime.CSharpScript,
                    imports: null, assemblies: null);
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