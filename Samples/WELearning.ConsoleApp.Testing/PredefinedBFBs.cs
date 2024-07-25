using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.ConsoleApp.Testing.Framework;
using WELearning.ConsoleApp.Testing.Framework.Bindings;

static class PredefinedBFBs
{
    public static readonly string AppFrameworkAssembly = typeof(AppFunctionFramework).Assembly.FullName;
    public static readonly IEnumerable<string> DefaultCsCompiledAssemblies;
    public static readonly IEnumerable<string> DefaultCsCompiledImports;
    public static readonly IEnumerable<string> DefaultCsScriptAssemblies;

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
        DelayInfiniteJs = CreateBlockDelayInfiniteJs();
        FactorialCsScript = CreateBlockFactorialCsScript();
        ConcatTwoStringsJs = CreateBlockConcatTwoStringsJs();
        CompilationErrorJs = CreateCompilationError();
        RuntimeExceptionJs = CreateRuntimeExceptionJs();
        RuntimeExceptionJsFromCs = CreateRuntimeExceptionJsFromCs();
        LogInputJs = CreateBlockLogInput();
        PrependEntryJs = CreateBlockPrependEntry();
        LastSeriesBeforeJs = CreateBlockLastSeriesBefore();
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
    public static readonly BasicBlockDef DelayInfiniteJs;
    public static readonly BasicBlockDef FactorialCsScript;
    public static readonly BasicBlockDef ConcatTwoStringsJs;
    public static readonly BasicBlockDef CompilationErrorJs;
    public static readonly BasicBlockDef RuntimeExceptionJs;
    public static readonly BasicBlockDef RuntimeExceptionJsFromCs;
    public static readonly BasicBlockDef LogInputJs;
    public static readonly BasicBlockDef PrependEntryJs;
    public static readonly BasicBlockDef LastSeriesBeforeJs;

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
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: string.Join(
                separator: Environment.NewLine,
                values: passThroughVars.Select(p => @$"OUT[""{p.Out.Name}""].Write({p.In.Name});")),
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
            var x = IN[""X""].AsDouble();
            var y = IN[""Y""].AsDouble();
            var result = x * y;
            OUT[""Result""].Write(result);
            EVENTS.Publish(""Completed"");
            ",
            handleInvalidScript: @$"
            CONSOLE.Trace(""Invalid arguments X, Y"");
            EVENTS.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = IN[""X""]; var y = IN[""Y""];
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockMultiplyCsCompiled()
    {
        return CreateBlockMultiply(
            runtime: ERuntime.CSharpCompiled,
            multiplyScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            var x = IN[""X""].AsDouble();
            var y = IN[""Y""].AsDouble();
            var result = x * y;
            OUT[""Result""].Write(result);
            EVENTS.Publish(""Completed"");
            "),
            handleInvalidScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            CONSOLE.Trace(""Invalid arguments X, Y"");
            EVENTS.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFunctionFramework>.WrapScript(@$"
            var x = IN[""X""]; var y = IN[""Y""];
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

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
            const x = IN.X; const y = IN.Y;
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
    private static BasicBlockDef CreateBlockAddCsScript()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpScript,
            addScript: @$"
            var x = IN[""X""].AsDouble();
            var y = IN[""Y""].AsDouble();
            var result = x + y;
            OUT[""Result""].Write(result);
            ",
            handleInvalidScript: @$"
            CONSOLE.Trace(""Invalid arguments X, Y"");
            EVENTS.Publish(""Completed"");
            ",
            invalidConditionScript: @$"
            var x = IN[""X""]; var y = IN[""Y""];
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockAddCsCompiled()
    {
        return CreateBlockAdd(
            runtime: ERuntime.CSharpCompiled,
            addScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            var x = IN[""X""].AsDouble();
            var y = IN[""Y""].AsDouble();
            var result = x + y;
            OUT[""Result""].Write(result);
            "),
            handleInvalidScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            CONSOLE.Trace(""Invalid arguments X, Y"");
            EVENTS.Publish(""Completed"");
            "),
            invalidConditionScript: BaseCompiledFunction<bool, AppFunctionFramework>.WrapScript(@$"
            var x = IN[""X""]; var y = IN[""Y""];
            return !x.ValueSet || !y.ValueSet || !x.IsNumeric || !y.IsNumeric;
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

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
            const x = IN.X; const y = IN.Y;
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

    #region Random
    private static BasicBlockDef CreateBlockRandomCsScript()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpScript,
            randomScript: @$"
            var result = FB.NextRandomDouble();
            OUT[""Result""].Write(result);
            EVENTS.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockRandomCsCompiled()
    {
        return CreateBlockRandom(
            runtime: ERuntime.CSharpCompiled,
            randomScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            var result = FB.NextRandomDouble();
            OUT[""Result""].Write(result);
            EVENTS.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static BasicBlockDef CreateBlockRandomJs()
    {
        return CreateBlockRandom(
            runtime: ERuntime.Javascript,
            randomScript: @$"Result = FB.NextRandomDouble()",
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
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: randomScript,
            runtime: runtime,
            imports: imports, assemblies: assemblies, types: null,
            signature: "Random", exported: true);
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
                tIdle2Running.DefaultOutputEvents = new[] { eCompleted.Name };

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
            var ms = IN[""Ms""].AsInt();
            await FB.DelayAsync(ms);
            EVENTS.Publish(""Completed"");
            ",
            imports: null, assemblies: DefaultCsScriptAssemblies
        );
    }

    private static BasicBlockDef CreateBlockDelayCsCompiled()
    {
        return CreateBlockDelay(
            runtime: ERuntime.CSharpCompiled,
            delayScript: BaseCompiledFunction<AppFunctionFramework>.WrapScript(@$"
            var ms = IN[""Ms""].AsInt();
            await FB.DelayAsync(ms);
            EVENTS.Publish(""Completed"");
            "),
            imports: DefaultCsCompiledImports, assemblies: DefaultCsCompiledAssemblies
        );
    }

    private static BasicBlockDef CreateBlockDelayJs()
    {
        return CreateBlockDelay(
            runtime: ERuntime.Javascript,
            delayScript: @$"
            await FB.DelayAsync(Ms);
            EVENTS.Publish('Completed');
            ",
            imports: null, assemblies: null
        );
    }

    private static BasicBlockDef CreateBlockDelayInfiniteJs()
    {
        return CreateBlockDelay(
            runtime: ERuntime.Javascript,
            delayScript: @$"
            await FB.DelayAsync(Ms);
            while (true) {{ var a = 1; }}
            ",
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
            id: Guid.NewGuid().ToString(),
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
                tIdle2Running.DefaultOutputEvents = new[] { eCompleted.Name };

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
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: @$"
            Result = X + Delimiter + Y;
            ",
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
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: @$"
            // [factor, result]
            INTERNAL[""State""].Write(new[] {{ 1, 1 }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var fLoop = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Loop",
            content: @$"
            var state = (int[])INTERNAL[""State""].Value;
            var factor = state[0] + 1;
            INTERNAL[""State""].Write(new[] {{ factor, state[1] * factor }});
            ",
            runtime: ERuntime.CSharpScript,
            imports: null, assemblies: assemblies, types: null);
        var fOutput = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Output",
            content: @$"
            var state = (int[])INTERNAL[""State""].Value;
            OUT[""Result""].Write(state[1]);                
            EVENTS.Publish(""{eCompleted.Name}"");
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
                    id: Guid.NewGuid().ToString(),
                    name: "Looping condition",
                    content: @$"
                        var n = IN[""N""].AsInt();
                        var state = (int[])INTERNAL[""State""].Value;
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

    #region Errors
    private static BasicBlockDef CreateCompilationError()
    {
        return CreateBlockSimple(
            id: "CompilationError", name: "Compilation error",
            content:
@"const a = 2; 
var 1 = 1;
let a = 5;"
        );
    }

    private static BasicBlockDef CreateRuntimeExceptionJs()
    {
        return CreateBlockSimple(
            id: "RuntimeExceptionJs", name: "Runtime exception (JS)",
            content:
@"const a = 2; 
function TestError() { throw new Error('This is a sample runtime exception!'); }
let b = 5;
var c = 10;
TestError();
c = 0;"
        );
    }

    private static BasicBlockDef CreateRuntimeExceptionJsFromCs()
    {
        return CreateBlockSimple(
            id: "RuntimeExceptionCs", name: "Runtime exception (C#)",
            content:
@"const a = 2; 
/* Test position */ FB.DemoException();
let b = 5;"
        );
    }
    #endregion

    private static BasicBlockDef CreateBlockLogInput()
    {
        return CreateBlockSimple(id: "LogInput", name: "Log input",
            content: @"
            const json = JSON.stringify(Data);
            console.log(json, Data.X, Data.Y, Data.Z);",
            variables: new Variable("Data", EDataType.Any, EVariableType.InOut));
    }

    private static BasicBlockDef CreateBlockPrependEntry()
    {
        return CreateBlockSimple(id: "PrependEntry", name: "Prepend entry with other",
            content: @"
            console.log(InputEntry.EntryKey);
            Result = InputEntry.Prepend(OtherName);", imports: null, importBlockIds: null,
            signature: "PrependEntry", exported: true,
            new Variable("InputEntry", EDataType.Reference, EVariableType.Input, objectType: nameof(ReadEntryBinding)),
            new Variable("OtherName", EDataType.String, EVariableType.Input),
            new Variable("Result", EDataType.String, EVariableType.Output));
    }

    private static BasicBlockDef CreateBlockLastSeriesBefore()
    {
        return CreateBlockSimple(id: "LastSeriesBefore", name: "Last metric value (sample for using reference binding method and reusing functions)",
            content: @"
            const series = await InputMetric.LastSeriesBefore(BeforeTime);
            const { Metric, Value, Timestamp } = series;
            console.log(Metric, '|', Value, '|', Timestamp);
            Result = series;
            ", imports: null, importBlockIds: null,
            signature: "LastSeriesBefore", exported: true,
            new Variable("InputMetric", EDataType.Reference, EVariableType.Input, objectType: nameof(ReadMetricBinding)),
            new Variable("BeforeTime", EDataType.DateTime, EVariableType.Input),
            new Variable("Result", EDataType.Object, EVariableType.Output));
    }

    public static BasicBlockDef CreateBlockSimple(
        string id, string name, string content,
        IEnumerable<string> imports = null,
        IEnumerable<string> importBlockIds = null,
        string signature = null, bool exported = false,
        params Variable[] variables)
    {
        var bSimple = new BasicBlockDef(id: id, name: name);
        bSimple.ImportBlockIds = importBlockIds;
        bSimple.Variables = variables;
        var inVars = variables.Where(v => v.CanInput()).Select(v => v.Name);
        var outVars = variables.Where(v => v.CanOutput()).Select(v => v.Name);
        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: inVars);
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: outVars);
        bSimple.Events = new[] { eTrigger, eCompleted };
        bSimple.DefaultTriggerEvent = eTrigger.Name;

        var fRun = new Function(
            id: Guid.NewGuid().ToString(),
            name: "Run",
            content: content,
            runtime: ERuntime.Javascript,
            imports: imports, assemblies: null, types: null,
            signature: signature, exported: exported);
        var functions = new[] { fRun };
        bSimple.Functions = functions;

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
            bSimple.ExecutionControlChart = execControl;
        }

        return bSimple;
    }
}