using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.Helpers;
using WELearning.Core.FunctionBlocks.Constants;

static class PredefinedBFBs
{
    private static readonly string AppFrameworkAssembly = typeof(AppFramework).Assembly.FullName;
    private static readonly IEnumerable<string> DefaultCsCompiledAssemblies;
    private static readonly IEnumerable<string> DefaultCsCompiledImports;
    private static readonly IEnumerable<string> DefaultCsScriptAssemblies;

    static PredefinedBFBs()
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
        ConcatTwoStringsJs = CreateBlockConcatTwoStringsJs();
    }

    public static readonly BasicBlockDef MultiplyCsCompiled;
    public static readonly BasicBlockDef MultiplyCsScript;
    public static readonly BasicBlockDef MultiplyJs;
    public static readonly BasicBlockDef AddCsCompiled;
    public static readonly BasicBlockDef AddCsScript;
    public static readonly BasicBlockDef AddJs;
    public static readonly BasicBlockDef RandomCsCompiled;
    public static readonly BasicBlockDef RandomCsScript;
    public static readonly BasicBlockDef RandomJs;
    public static readonly BasicBlockDef DelayCsCompiled;
    public static readonly BasicBlockDef DelayCsScript;
    public static readonly BasicBlockDef DelayJs;
    public static readonly BasicBlockDef FactorialCsScript;
    public static readonly BasicBlockDef ConcatTwoStringsJs;

    public static BasicBlockDef CreateInOutBlock(params Variable[] variables)
    {
        if (variables.Any(v => v.VariableType != EVariableType.InOut))
            throw new ArgumentException("Invalid binding type!");
        var bInOut = new BasicBlockDef(id: $"InOut-{Guid.NewGuid()}", name: "InOut block");
        bInOut.Variables = variables;
        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: variables.Select(v => v.Name).ToArray());
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: variables.Select(v => v.Name).ToArray());
        bInOut.Events = new[] { eTrigger, eCompleted };
        bInOut.DefaultTriggerEvent = eTrigger.Name;

        {
            var execControl = new BlockECC();

            var sInit = new BlockState("Init");
            var sTriggered = new BlockState("Triggered");
            var states = new[] { sInit, sTriggered };

            var transitions = new List<BlockStateTransition>();
            {
                var tInit2Triggered = new BlockStateTransition(fromState: sInit.Name, toState: sTriggered.Name, triggerEventName: eTrigger.Name)
                {
                    DefaultOutputEvents = new[] { eCompleted.Name }
                };
                var tTriggered2Triggered = new BlockStateTransition(fromState: sTriggered.Name, toState: sTriggered.Name, triggerEventName: eTrigger.Name)
                {
                    DefaultOutputEvents = new[] { eCompleted.Name }
                };
                transitions.Add(tInit2Triggered);
                transitions.Add(tTriggered2Triggered);
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sInit.Name;
            bInOut.ExecutionControlChart = execControl;
        }

        return bInOut;
    }

    public static BasicBlockDef CreatePassThroughBlock(params (Variable In, Variable Out)[] passThroughVars)
    {
        var bPassThrough = new BasicBlockDef(id: $"PassThrough-{Guid.NewGuid()}", name: "Pass through block");
        var inVariables = new List<string>();
        var outVariables = new List<string>();
        var allVariables = new List<Variable>();
        foreach (var var in passThroughVars)
        {
            allVariables.Add(var.In);
            allVariables.Add(var.Out);
            inVariables.Add(var.In.Name);
            outVariables.Add(var.Out.Name);
        }
        bPassThrough.Variables = allVariables;
        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: inVariables);
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: outVariables);
        bPassThrough.Events = new[] { eTrigger, eCompleted };
        bPassThrough.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Run",
            name: "Run",
            content: JavascriptHelper.WrapModuleFunction(
                script: string.Join(
                    separator: Environment.NewLine,
                    values: passThroughVars.Select(p => @$"await FB.Out(""{p.Out.Name}"").Write({p.In.Name});")),
                inputVariables: JavascriptHelper.GetInputVariableNames(bPassThrough.Variables)
            ),
            runtime: ERuntime.Javascript,
            imports: null, assemblies: null, types: null);
        bPassThrough.Functions = new[] { fRun };

        {
            var execControl = new BlockECC();

            var sInit = new BlockState("Init");
            var sTriggered = new BlockState("Triggered");
            var states = new[] { sInit, sTriggered };

            var transitions = new List<BlockStateTransition>();
            {
                var tInit2Triggered = new BlockStateTransition(fromState: sInit.Name, toState: sTriggered.Name, triggerEventName: eTrigger.Name)
                {
                    DefaultOutputEvents = new[] { eCompleted.Name },
                    ActionFunctionIds = new[] { fRun.Id }
                };
                var tTriggered2Triggered = new BlockStateTransition(fromState: sTriggered.Name, toState: sTriggered.Name, triggerEventName: eTrigger.Name)
                {
                    DefaultOutputEvents = new[] { eCompleted.Name },
                    ActionFunctionIds = new[] { fRun.Id }
                };
                transitions.Add(tInit2Triggered);
                transitions.Add(tTriggered2Triggered);
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sInit.Name;
            bPassThrough.ExecutionControlChart = execControl;
        }

        return bPassThrough;
    }

    #region Multiply
    private static BasicBlockDef CreateBlockMultiplyCsScript()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpScript,
            multiplyScript: @$"
            var x = FB.In(""X"").AsDouble();
            var y = FB.In(""Y"").AsDouble();
            var result = x * y;
            await FB.Out(""Result"").Write(result);
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

    private static BasicBlockDef CreateBlockMultiplyCsCompiled()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpCompiled,
            multiplyScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var x = FB.In(""X"").AsDouble();
            var y = FB.In(""Y"").AsDouble();
            var result = x * y;
            await FB.Out(""Result"").Write(result);
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

    private static BasicBlockDef CreateBlockMultiplyJs()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.Javascript,
            multiplyScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.In(""X"").AsDouble();
            const y = FB.In(""Y"").AsDouble();
            const result = x * y;
            await FB.Out(""Result"").Write(result);
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
            id: "Run",
            name: "Run",
            content: multiplyScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var fHandleInvalid = new Function(
            id: "HandleInvalid",
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
                    id: "InvalidCondition",
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
    private static BasicBlockDef CreateBlockAddCsScript()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpScript,
            addScript: @$"
            var x = FB.In(""X"").AsDouble();
            var y = FB.In(""Y"").AsDouble();
            var result = x + y;
            await FB.Out(""Result"").Write(result);
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

    private static BasicBlockDef CreateBlockAddCsCompiled()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpCompiled,
            addScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var x = FB.In(""X"").AsDouble();
            var y = FB.In(""Y"").AsDouble();
            var result = x + y;
            await FB.Out(""Result"").Write(result);
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

    private static BasicBlockDef CreateBlockAddJs()
    {
        return CreateBlockAdd(
            runtime: ERuntime.Javascript,
            addScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const x = FB.In(""X"").AsDouble();
            const y = FB.In(""Y"").AsDouble();
            const result = x + y;
            await FB.Out(""Result"").Write(result);
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
            id: "Run",
            name: "Run",
            content: addScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var fHandleInvalid = new Function(
            id: "HandleInvalid",
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
                    id: "InvalidCondition",
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
            bAdd.ExecutionControlChart = execControl;
        }

        return bAdd;
    }
    #endregion

    #region Random
    private static BasicBlockDef CreateBlockRandomCsScript()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpScript,
            randomScript: @$"
            var result = FB.NextRandomDouble();
            await FB.Out(""Result"").Write(result);
            await FB.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockRandomCsCompiled()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpCompiled,
            randomScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var result = FB.NextRandomDouble();
            await FB.Out(""Result"").Write(result);
            await FB.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static BasicBlockDef CreateBlockRandomJs()
    {
        return CreateBlockRandom(
            runtime: ERuntime.Javascript,
            randomScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const result = FB.NextRandomDouble();
            await FB.Out(""Result"").Write(result);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static BasicBlockDef CreateBlockRandom(ERuntime runtime,
        string randomScript, IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bRandom = new BasicBlockDef(id: $"RandomDouble{runtime}", name: $"Random double ({runtime})");

        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        bRandom.Variables = new[] { oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: Array.Empty<string>());
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bRandom.Events = new[] { eTrigger, eCompleted };
        bRandom.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Run",
            name: "Run",
            content: randomScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var functions = new[] { fRun };
        bRandom.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var states = new[] { sIdle, sRunning };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };

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
    private static BasicBlockDef CreateBlockDelayCsScript()
    {
        return CreateBlockDelay(
            runtime: ERuntime.CSharpScript,
            delayScript: @$"
            var ms = FB.In(""Ms"").AsInt();
            await FB.DelayAsync(ms);
            await FB.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockDelayCsCompiled()
    {
        return CreateBlockDelay(
            runtime: ERuntime.CSharpCompiled,
            delayScript: BaseCompiledFunction<AppFramework>.WrapScript(@$"
            var ms = FB.In(""Ms"").AsInt();
            await FB.DelayAsync(ms);
            await FB.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static BasicBlockDef CreateBlockDelayJs()
    {
        return CreateBlockDelay(
            runtime: ERuntime.Javascript,
            delayScript: JavascriptHelper.WrapModuleFunction(@$"
            const FB = _FB_.FB;
            const ms = FB.In(""Ms"").AsInt();
            FB.Delay(ms);
            await FB.Publish(""Completed"");
            "),
            imports: null, assemblies: null
        );
    }

    private static BasicBlockDef CreateBlockDelay(ERuntime runtime,
        string delayScript, IEnumerable<string> imports, IEnumerable<string> assemblies)
    {
        var bDelay = new BasicBlockDef(id: $"Delay{runtime}", name: $"Delay ({runtime})");

        var iMs = new Variable("Ms", EDataType.Int, EVariableType.Input);
        bDelay.Variables = new[] { iMs };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iMs.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: Array.Empty<string>());
        bDelay.Events = new[] { eTrigger, eCompleted };
        bDelay.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Run",
            name: "Run",
            content: delayScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null);
        var functions = new[] { fRun };
        bDelay.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var states = new[] { sIdle, sRunning };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };

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

    private static BasicBlockDef CreateBlockConcatTwoStringsJs()
    {
        var bConcat = new BasicBlockDef(id: $"ConcatTwoStrings", name: $"Concat two strings");

        var iX = new Variable("X", EDataType.String, EVariableType.Input);
        var iY = new Variable("Y", EDataType.String, EVariableType.Input);
        var iDeli = new Variable("Delimiter", EDataType.String, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.String, EVariableType.Output);
        bConcat.Variables = new[] { iX, iY, iDeli, oResult };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iX.Name, iY.Name, iDeli.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bConcat.Events = new[] { eTrigger, eCompleted };
        bConcat.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Run",
            name: "Run",
            content: JavascriptHelper.WrapModuleFunction(
                script: @$"
                var result = X + Delimiter + Y;
                await FB.Out(""Result"").Write(result);
                ",
                inputVariables: JavascriptHelper.GetInputVariableNames(bConcat.Variables)
            ),
            runtime: ERuntime.Javascript,
            imports: null, assemblies: null, types: null);
        var functions = new[] { fRun };
        bConcat.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var states = new[] { sIdle, sRunning };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };
                tIdle2Running.DefaultOutputEvents = new[] { eCompleted.Name };

                transitions.Add(tIdle2Running);
                transitions.Add(new(fromState: sRunning.Name, toState: sIdle.Name));
            }

            execControl.States = states;
            execControl.StateTransitions = transitions;
            execControl.InitialState = sIdle.Name;
            bConcat.ExecutionControlChart = execControl;
        }

        return bConcat;
    }

    private static BasicBlockDef CreateBlockFactorialCsScript()
    {
        var runtime = ERuntime.CSharpScript;
        var assemblies = new[] { AppFrameworkAssembly };
        var bFactorial = new BasicBlockDef(id: $"Factorial{runtime}", name: $"Factorial n! ({runtime})");

        var iN = new Variable("N", EDataType.Int, EVariableType.Input);
        var oResult = new Variable("Result", EDataType.Numeric, EVariableType.Output);
        var itnState = new Variable("State", EDataType.Object, EVariableType.Internal);
        bFactorial.Variables = new[] { iN, oResult, itnState };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iN.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oResult.Name });
        bFactorial.Events = new[] { eTrigger, eCompleted };
        bFactorial.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: "Run",
            name: "Run",
            content: @$"
            // [factor, result]
            await FB.Internal(""State"").Write(new[] {{ 1, 1 }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var fLoop = new Function(
            id: "Loop",
            name: "Loop",
            content: @$"
            var state = (int[])FB.Internal(""State"").Value;
            var factor = state[0] + 1;
            await FB.Internal(""State"").Write(new[] {{ factor, state[1] * factor }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var fOutput = new Function(
            id: "Output",
            name: "Output",
            content: @$"
            var state = (int[])FB.Internal(""State"").Value;
            await FB.Out(""Result"").Write(state[1]);                
            await FB.Publish(""{eCompleted.Name}"");
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var functions = new[] { fRun, fLoop, fOutput };
        bFactorial.Functions = functions;

        {
            var execControl = new BlockECC();

            var sIdle = new BlockState("Idle");
            var sRunning = new BlockState("Running");
            var sLooping = new BlockState("Looping");
            var sOutput = new BlockState("Output");
            var states = new[] { sIdle, sRunning, sLooping, sOutput };

            var transitions = new List<BlockStateTransition>();
            {
                var tIdle2Running = new BlockStateTransition(fromState: sIdle.Name, toState: sRunning.Name, triggerEventName: eTrigger.Name);
                tIdle2Running.ActionFunctionIds = new[] { fRun.Id };

                var fLoopingCondition = new Function(
                    id: "LoopingCondition",
                    name: "Looping condition",
                    content: @$"
                        var n = FB.In(""N"").AsInt();
                        var state = (int[])FB.Internal(""State"").Value;
                        return state[0] < n;
                    ",
                    runtime: ERuntime.CSharpScript,
                    imports: null, assemblies: assemblies, types: null
                );
                var tRunning2Looping = new BlockStateTransition(fromState: sRunning.Name, toState: sLooping.Name);
                tRunning2Looping.TriggerCondition = fLoopingCondition;
                tRunning2Looping.ActionFunctionIds = new[] { fLoop.Id };

                var tLooping2Looping = new BlockStateTransition(fromState: sLooping.Name, toState: sLooping.Name);
                tLooping2Looping.TriggerCondition = fLoopingCondition;
                tLooping2Looping.ActionFunctionIds = new[] { fLoop.Id };

                var tRunning2Output = new BlockStateTransition(fromState: sRunning.Name, toState: sOutput.Name);
                tRunning2Output.ActionFunctionIds = new[] { fOutput.Id };
                var tLooping2Output = new BlockStateTransition(fromState: sLooping.Name, toState: sOutput.Name);
                tLooping2Output.ActionFunctionIds = new[] { fOutput.Id };

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