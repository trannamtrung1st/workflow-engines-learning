using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.Helpers;

static class PredefinedBlocks
{
    private static readonly Assembly AppFrameworkAssembly = typeof(AppFramework).Assembly;
    public static readonly FunctionBlock MultiplyCsScript = CreateBlockMultiplyCsScript();
    public static readonly FunctionBlock MultiplyCsCompiled = CreateBlockMultiplyCsCompiled();
    public static readonly FunctionBlock AddCsScript = CreateBlockAddCsScript();
    public static readonly FunctionBlock AddJs = CreateBlockAddJs();
    public static readonly FunctionBlock RandomCsScript = CreateBlockRandomCsScript();
    public static readonly FunctionBlock FactorialCsScript = CreateBlockFactorialCsScript();

    #region Multiply
    private static FunctionBlock CreateBlockMultiplyCsScript()
    {
        var assemblies = new[] { AppFrameworkAssembly };
        return CreateBlockMultiplyCs(
            runtime: ERuntime.CSharpScript,
            multiplyScriptProvider: s => s,
            handleInvalidScriptProvider: s => s,
            invalidConditionScriptProvider: s => s,
            imports: null, assemblies: assemblies
        );
    }

    private static FunctionBlock CreateBlockMultiplyCsCompiled()
    {
        var mscorlib = typeof(object).GetTypeInfo().Assembly;
        var assemblies = new List<Assembly> { mscorlib, AppFrameworkAssembly };
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
            runtime: ERuntime.CSharpCompiled,
            multiplyScriptProvider: s => BaseCompiledFunction<AppFramework>.WrapScript(s),
            handleInvalidScriptProvider: s => BaseCompiledFunction<AppFramework>.WrapScript(s),
            invalidConditionScriptProvider: s => BaseCompiledFunction<bool, AppFramework>.WrapScript(s),
            imports: new[]
            {
                "System.Threading",
                "System.Threading.Tasks",
                "WELearning.Core.FunctionBlocks.Framework",
            }, assemblies: assemblies
        );
    }

    private static FunctionBlock CreateBlockMultiplyCs(
        ERuntime runtime,
        Func<string, string> multiplyScriptProvider,
        Func<string, string> handleInvalidScriptProvider,
        Func<string, string> invalidConditionScriptProvider,
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
                        return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
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

    private static FunctionBlock CreateBlockAddCsScript()
    {
        return CreateBlockAddBase(
            runtime: ERuntime.CSharpScript,
            addContent: @$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x + y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            ",
            handleInvalidContent: @$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            ",
            invalidConditionContent: @$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            assemblies: new[] { AppFrameworkAssembly }
        );
    }

    private static FunctionBlock CreateBlockAddJs()
    {
        return CreateBlockAddBase(
            runtime: ERuntime.Javascript,
            addContent: JavascriptHelper.WrapTopLevelAsyncCall(@$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x + y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidContent: JavascriptHelper.WrapTopLevelAsyncCall(@$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionContent: @$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            assemblies: null
        );
    }

    private static FunctionBlock CreateBlockAddBase(ERuntime runtime,
        string addContent, string handleInvalidContent, string invalidConditionContent,
        IEnumerable<Assembly> assemblies)
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
            content: addContent,
            runtime: runtime,
            imports: null, assemblies: assemblies);
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: handleInvalidContent,
            runtime: runtime,
            imports: null, assemblies: assemblies);
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
                    content: invalidConditionContent,
                    runtime: runtime,
                    imports: null, assemblies: assemblies);
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

    private static FunctionBlock CreateBlockRandomCsScript()
    {
        var assemblies = new[] { AppFrameworkAssembly };
        var bRandom = new FunctionBlock(id: "RandomDouble", name: "Random double");

        bRandom.Inputs = Array.Empty<Variable>();

        var oResult = new Variable("Result", EDataType.Numeric);
        var outputs = new[] { oResult };
        bRandom.Outputs = outputs;

        var inputEvents = new List<BlockEvent>();
        var eRun = new BlockEvent(name: "Run", variableNames: Array.Empty<string>());
        inputEvents.Add(eRun);
        bRandom.InputEvents = inputEvents;
        bRandom.DefaultTriggerEvent = eRun.Name;

        var outputEvents = new List<BlockEvent>();
        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        outputEvents.Add(eCompleted);
        bRandom.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            var result = FB.NextRandomDouble();
            await FB.Set(""{oResult.Name}"", result);
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
        var logics = new[] { lRun };
        bRandom.Logics = logics;

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
            bRandom.ExecutionControlChart = execControl;
        }

        return bRandom;
    }

    private static FunctionBlock CreateBlockFactorialCsScript()
    {
        var assemblies = new[] { AppFrameworkAssembly };
        var bFactorial = new FunctionBlock(id: "Factorial", name: "Factorial n!");

        var iN = new Variable("N", EDataType.Int);
        bFactorial.Inputs = new[] { iN };

        var oResult = new Variable("Result", EDataType.Numeric);
        var outputs = new[] { oResult };
        bFactorial.Outputs = outputs;

        var eRun = new BlockEvent(name: "Run", variableNames: new[] { iN.Name });
        var inputEvents = new[] { eRun };
        bFactorial.InputEvents = inputEvents;
        bFactorial.DefaultTriggerEvent = eRun.Name;

        var eCompleted = new BlockEvent(name: "Completed", variableNames: new[] { oResult.Name });
        var outputEvents = new[] { eCompleted };
        bFactorial.OutputEvents = outputEvents;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            // [factor, result]
            await FB.Set(""State"", new[] {{ 1, 1 }}, isInternal: true);
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
        var lLoop = new Logic(
            id: "Loop",
            name: "Loop",
            content: @$"
            var state = (int[])FB.Get(""State"", isInternal: true).Value;
            var factor = state[0] + 1;
            await FB.Set(""State"", new[] {{ factor, state[1] * factor }}, isInternal: true);
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
        var lOutput = new Logic(
            id: "Output",
            name: "Output",
            content: @$"
            var state = (int[])FB.Get(""State"", isInternal: true).Value;
            await FB.Set(""{oResult.Name}"", state[1]);                
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies);
        var logics = new[] { lRun, lLoop, lOutput };
        bFactorial.Logics = logics;

        {
            var execControl = new BlockExecutionControlChart();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sLooping = new BlockState("Looping");
            var sOutput = new BlockState("Output");
            var states = new[] { sIdle, sRunning, sLooping, sOutput };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eRun.Name);
                tIdle2Running.ActionLogicId = lRun.Id;

                var loopingCondition = new Logic(
                    id: "LoopingCondition",
                    name: "Looping condition",
                    content: @$"
                        var n = (int)FB.Get(""{iN.Name}"").Value;
                        var state = (int[])FB.Get(""State"", isInternal: true).Value;
                        return state[0] < n;
                    ",
                    runtime: ERuntime.CSharpScript,
                    imports: null, assemblies: assemblies
                );
                var tRunning2Looping = new BlockStateTransition(fromState: sRunning.Name, toState: sLooping.Name);
                tRunning2Looping.TriggerCondition = loopingCondition;
                tRunning2Looping.ActionLogicId = lLoop.Id;

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name);
                tLooping2Looping.TriggerCondition = loopingCondition;
                tLooping2Looping.ActionLogicId = lLoop.Id;

                var tRunning2Output = new BlockStateTransition(fromState: sRunning.Name, toState: sOutput.Name);
                tRunning2Output.ActionLogicId = lOutput.Id;
                var tLooping2Output = new BlockStateTransition(fromState: sLooping.Name, toState: sOutput.Name);
                tLooping2Output.ActionLogicId = lOutput.Id;

                transitions.Add(tIdle2Running);
                transitions.Add(tRunning2Looping);
                transitions.Add(tLooping2Looping);
                transitions.Add(tRunning2Output);
                transitions.Add(tLooping2Output);
                transitions.Add(new(fromState: sOutput.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bFactorial.ExecutionControlChart = execControl;
        }

        return bFactorial;
    }

}