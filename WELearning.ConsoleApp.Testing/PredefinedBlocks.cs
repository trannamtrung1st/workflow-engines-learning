using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.Helpers;

static class PredefinedBlocks
{
    private static readonly Assembly AppFrameworkAssembly = typeof(AppFramework).Assembly;
    private static readonly IEnumerable<Assembly> DefaultCsCompiledAssemblies;
    private static readonly IEnumerable<string> DefaultCsCompiledImports;
    private static readonly IEnumerable<Assembly> DefaultCsScriptAssemblies;

    static PredefinedBlocks()
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
        var imports = new[]
        {
            "System.Threading",
            "System.Threading.Tasks",
            typeof(BaseCompiledFunction<>).Namespace,
        };

        DefaultCsCompiledAssemblies = assemblies;
        DefaultCsCompiledImports = imports;
        DefaultCsScriptAssemblies = new[] { AppFrameworkAssembly };

        MultiplyCsCompiled = CreateBlockMultiplyCsCompiled();
        MultiplyCsScript = CreateBlockMultiplyCsScript();
        MultiplyJs = CreateBlockMultiplyJs();
        AddCsCompiled = CreateBlockAddCsCompiled();
        AddCsScript = CreateBlockAddCsScript();
        AddJs = CreateBlockAddJs();
        RandomCsCompiled = CreateBlockRandomCsCompiled();
        RandomCsScript = CreateBlockRandomCsScript();
        RandomJs = CreateBlockRandomJs();
        DelayCsCompiled = CreateBlockDelayCsCompiled();
        DelayCsScript = CreateBlockDelayCsScript();
        DelayJs = CreateBlockDelayJs();
        FactorialCsScript = CreateBlockFactorialCsScript();
    }

    public static readonly FunctionBlock MultiplyCsCompiled;
    public static readonly FunctionBlock MultiplyCsScript;
    public static readonly FunctionBlock MultiplyJs;
    public static readonly FunctionBlock AddCsCompiled;
    public static readonly FunctionBlock AddCsScript;
    public static readonly FunctionBlock AddJs;
    public static readonly FunctionBlock RandomCsCompiled;
    public static readonly FunctionBlock RandomCsScript;
    public static readonly FunctionBlock RandomJs;
    public static readonly FunctionBlock DelayCsCompiled;
    public static readonly FunctionBlock DelayCsScript;
    public static readonly FunctionBlock DelayJs;
    public static readonly FunctionBlock FactorialCsScript;

    #region Multiply
    private static FunctionBlock CreateBlockMultiplyCsScript()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpScript,
            multiplyScript: @$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x * y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            ",
            handleInvalidScript: @$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static FunctionBlock CreateBlockMultiplyCsCompiled()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpCompiled,
            multiplyScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x * y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFramework>.WrapScript(@$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static FunctionBlock CreateBlockMultiplyJs()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.Javascript,
            multiplyScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.GetDouble(""X"");
            const y = FB.GetDouble(""Y"");
            const result = x * y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.Get(""X""); const y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockMultiply(
        ERuntime runtime,
        string multiplyScript,
        string handleInvalidScript,
        string invalidConditionScript,
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
            content: multiplyScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: handleInvalidScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
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
                    content: invalidConditionScript,
                    runtime: runtime,
                    imports: imports, assemblies: assemblies, types: null);
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

    #region Add
    private static FunctionBlock CreateBlockAddCsScript()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpScript,
            addScript: @$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x + y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            ",
            handleInvalidScript: @$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static FunctionBlock CreateBlockAddCsCompiled()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpCompiled,
            addScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var x = FB.GetDouble(""X"");
            var y = FB.GetDouble(""Y"");
            var result = x + y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFramework>.WrapScript(@$"
            var x = FB.Get(""X""); var y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static FunctionBlock CreateBlockAddJs()
    {
        return CreateBlockAdd(
            runtime: ERuntime.Javascript,
            addScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.GetDouble(""X"");
            const y = FB.GetDouble(""Y"");
            const result = x + y;
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.Get(""X""); const y = FB.Get(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockAdd(ERuntime runtime,
        string addScript, string handleInvalidScript, string invalidConditionScript,
        IEnumerable<string> imports, IEnumerable<Assembly> assemblies)
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
            content: addScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var lHandleInvalid = new Logic(
            id: "HandleInvalid",
            name: "Handle invalid",
            content: handleInvalidScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
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
                    content: invalidConditionScript,
                    runtime: runtime,
                    imports: imports, assemblies: assemblies, types: null);
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
    #endregion

    #region Random
    private static FunctionBlock CreateBlockRandomCsScript()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpScript,
            randomScript: @$"
            var result = FB.NextRandomDouble();
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static FunctionBlock CreateBlockRandomCsCompiled()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpCompiled,
            randomScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var result = FB.NextRandomDouble();
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static FunctionBlock CreateBlockRandomJs()
    {
        return CreateBlockRandom(
            runtime: ERuntime.Javascript,
            randomScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const result = FB.NextRandomDouble();
            await FB.Set(""Result"", result);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockRandom(ERuntime runtime,
        string randomScript, IEnumerable<string> imports, IEnumerable<Assembly> assemblies)
    {
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
            content: randomScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
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
    #endregion

    #region Delay
    private static FunctionBlock CreateBlockDelayCsScript()
    {
        return CreateBlockDelay(
            runtime: ERuntime.CSharpScript,
            delayScript: @$"
            var ms = FB.GetInt(""Ms"");
            await FB.DelayAsync(ms);
            await FB.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static FunctionBlock CreateBlockDelayCsCompiled()
    {
        return CreateBlockDelay(
            runtime: ERuntime.CSharpCompiled,
            delayScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var ms = FB.GetInt(""Ms"");
            await FB.DelayAsync(ms);
            await FB.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static FunctionBlock CreateBlockDelayJs()
    {
        return CreateBlockDelay(
            runtime: ERuntime.Javascript,
            delayScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const ms = FB.GetInt(""Ms"");
            FB.Delay(ms);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockDelay(ERuntime runtime,
        string delayScript, IEnumerable<string> imports, IEnumerable<Assembly> assemblies)
    {
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
            content: delayScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
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

    #endregion

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
            imports: null, assemblies: assemblies, types: null);
        var lLoop = new Logic(
            id: "Loop",
            name: "Loop",
            content: @$"
            var state = (int[])FB.Get(""State"", isInternal: true).Value;
            var factor = state[0] + 1;
            await FB.Set(""State"", new[] {{ factor, state[1] * factor }}, isInternal: true);
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var lOutput = new Logic(
            id: "Output",
            name: "Output",
            content: @$"
            var state = (int[])FB.Get(""State"", isInternal: true).Value;
            await FB.Set(""{oResult.Name}"", state[1]);                
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
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
                    imports: null, assemblies: assemblies, types: null
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