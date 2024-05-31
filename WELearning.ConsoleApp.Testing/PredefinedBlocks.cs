using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.Helpers;
using WELearning.Core.FunctionBlocks.Constants;

static class PredefinedBlocks
{
    private static readonly string AppFrameworkAssembly = typeof(AppFramework).Assembly.FullName;
    private static readonly IEnumerable<string> DefaultCsCompiledAssemblies;
    private static readonly IEnumerable<string> DefaultCsCompiledImports;
    private static readonly IEnumerable<string> DefaultCsScriptAssemblies;

    static PredefinedBlocks()
    {
        var mscorlib = typeof(object).GetTypeInfo().Assembly.FullName;
        var assemblies = new List<string> { mscorlib, AppFrameworkAssembly };
        var dllNames = new string[]
        {
            "System.Runtime",
            // "System.Net.Http",
            // "System.Threading",
            // "System.Core",
            // "System.Linq",
            // "netstandard",
        };
        assemblies.AddRange(dllNames);
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

    public static FunctionBlock CreateInOutBlock(params Variable[] variables)
    {
        if (variables.Any(v => v.VariableType != EVariableType.InOut))
            throw new ArgumentException("Invalid binding type!");
        return new FunctionBlock(id: "InOut", name: "InOut block") { Variables = variables };
    }

    public static FunctionBlock CreateInputBlock(params Variable[] variables)
    {
        if (variables.Any(v => v.VariableType != EVariableType.Output))
            throw new ArgumentException("Invalid binding type!");
        return new FunctionBlock(id: "Input", name: "Input block") { Variables = variables };
    }

    public static FunctionBlock CreateOutputBlock(params Variable[] variables)
    {
        if (variables.Any(v => v.VariableType != EVariableType.Input))
            throw new ArgumentException("Invalid binding type!");
        var bOutput = new FunctionBlock(id: "Output", name: "Output block");
        bOutput.Variables = variables;
        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: variables.Select(v => v.Name).ToArray());
        bOutput.Events = new[] { eRun };
        bOutput.DefaultTriggerEvent = eRun.Name;
        return bOutput;
    }

    #region Multiply
    private static FunctionBlock CreateBlockMultiplyCsScript()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpScript,
            multiplyScript: @$"
            var x = FB.In(""X"").ToDouble();
            var y = FB.In(""Y"").ToDouble();
            var result = x * y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            ",
            handleInvalidScript: @$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = FB.In(""X""); var y = FB.In(""Y"");
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
            var x = FB.In(""X"").ToDouble();
            var y = FB.In(""Y"").ToDouble();
            var result = x * y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFramework>.WrapScript(@$"
            var x = FB.In(""X""); var y = FB.In(""Y"");
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
            const x = FB.In(""X"").ToDouble();
            const y = FB.In(""Y"").ToDouble();
            const result = x * y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.In(""X""); const y = FB.In(""Y"");
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
        IEnumerable<string> assemblies)
    {
        var bMultiply = new FunctionBlock(id: "Multiply", name: "Multiply X * Y");

        var iX = new Variable("X", EDataType.Numeric, EVariableType.Input);
        var iY = new Variable("Y", EDataType.Numeric, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bMultiply.Variables = new[] { iX, iY, oResult };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: new[] { iX.Name, iY.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bMultiply.Events = new[] { eRun, eCompleted };
        bMultiply.DefaultTriggerEvent = eRun.Name;

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
            var x = FB.In(""X"").ToDouble();
            var y = FB.In(""Y"").ToDouble();
            var result = x + y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            ",
            handleInvalidScript: @$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = FB.In(""X""); var y = FB.In(""Y"");
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
            var x = FB.In(""X"").ToDouble();
            var y = FB.In(""Y"").ToDouble();
            var result = x + y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFramework>.WrapScript(@$"
            var x = FB.In(""X""); var y = FB.In(""Y"");
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
            const x = FB.In(""X"").ToDouble();
            const y = FB.In(""Y"").ToDouble();
            const result = x + y;
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            "),
            handleInvalidScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            FB.LogWarning(""Invalid arguments X, Y"");
            await FB.Publish(""Completed"");
            "),
            invalidConditionScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.In(""X""); const y = FB.In(""Y"");
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockAdd(ERuntime runtime,
        string addScript, string handleInvalidScript, string invalidConditionScript,
        IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bAdd = new FunctionBlock(id: "Add", name: "Add X + Y");

        var iX = new Variable("X", EDataType.Numeric, EVariableType.Input);
        var iY = new Variable("Y", EDataType.Numeric, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bAdd.Variables = new[] { iX, iY, oResult };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: new[] { iX.Name, iY.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bAdd.Events = new[] { eRun, eCompleted };
        bAdd.DefaultTriggerEvent = eRun.Name;

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
            await FB.Out(""Result"").Set(result);
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
            await FB.Out(""Result"").Set(result);
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
            await FB.Out(""Result"").Set(result);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockRandom(ERuntime runtime,
        string randomScript, IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bRandom = new FunctionBlock(id: "RandomDouble", name: "Random double");

        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bRandom.Variables = new[] { oResult };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: Array.Empty<string>());
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bRandom.Events = new[] { eRun, eCompleted };
        bRandom.DefaultTriggerEvent = eRun.Name;

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
                tIdle2Running.ActionLogicIds = new[] { lRun.Id };

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
            var ms = FB.In(""Ms"").ToInt();
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
            var ms = FB.In(""Ms"").ToInt();
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
            const ms = FB.In(""Ms"").ToInt();
            FB.Delay(ms);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static FunctionBlock CreateBlockDelay(ERuntime runtime,
        string delayScript, IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bDelay = new FunctionBlock(id: "Delay", name: "Delay");

        var iMs = new Variable("Ms", EDataType.Int, EVariableType.Input);
        bDelay.Variables = new[] { iMs };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: new[] { iMs.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: Array.Empty<string>());
        bDelay.Events = new[] { eRun, eCompleted };
        bDelay.DefaultTriggerEvent = eRun.Name;

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
                tIdle2Running.ActionLogicIds = new[] { lRun.Id };

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

        var iN = new Variable("N", EDataType.Int, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        var itnState = new Variable("State", EDataType.Object, EVariableType.Internal);
        bFactorial.Variables = new[] { iN, oResult, itnState };

        var eRun = new BlockEvent(isInput: true, name: "Run", variableNames: new[] { iN.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bFactorial.Events = new[] { eRun, eCompleted };
        bFactorial.DefaultTriggerEvent = eRun.Name;

        var lRun = new Logic(
            id: "Run",
            name: "Run",
            content: @$"
            // [factor, result]
            await FB.Internal(""State"").Set(new[] {{ 1, 1 }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var lLoop = new Logic(
            id: "Loop",
            name: "Loop",
            content: @$"
            var state = (int[])FB.Internal(""State"").Value;
            var factor = state[0] + 1;
            await FB.Internal(""State"").Set(new[] {{ factor, state[1] * factor }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var lOutput = new Logic(
            id: "Output",
            name: "Output",
            content: @$"
            var state = (int[])FB.Internal(""State"").Value;
            await FB.Out(""Result"").Set(state[1]);                
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
                tIdle2Running.ActionLogicIds = new[] { lRun.Id };

                var loopingCondition = new Logic(
                    id: "LoopingCondition",
                    name: "Looping condition",
                    content: @$"
                        var n = FB.In(""N"").ToInt();
                        var state = (int[])FB.Internal(""State"").Value;
                        return state[0] < n;
                    ",
                    runtime: ERuntime.CSharpScript,
                    imports: null, assemblies: assemblies, types: null
                );
                var tRunning2Looping = new BlockStateTransition(fromState: sRunning.Name, toState: sLooping.Name);
                tRunning2Looping.TriggerCondition = loopingCondition;
                tRunning2Looping.ActionLogicIds = new[] { lLoop.Id };

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name);
                tLooping2Looping.TriggerCondition = loopingCondition;
                tLooping2Looping.ActionLogicIds = new[] { lLoop.Id };

                var tRunning2Output = new BlockStateTransition(fromState: sRunning.Name, toState: sOutput.Name);
                tRunning2Output.ActionLogicIds = new[] { lOutput.Id };
                var tLooping2Output = new BlockStateTransition(fromState: sLooping.Name, toState: sOutput.Name);
                tLooping2Output.ActionLogicIds = new[] { lOutput.Id };

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