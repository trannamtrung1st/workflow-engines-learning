using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Helpers;

public static class BlockHelper
{
    public static BasicBlockDef CreateInOutBlock(params Variable[] variables)
        => CreateInOutBlock(id: $"InOut-{Guid.NewGuid()}", name: "InOut block", variables);

    public static BasicBlockDef CreateInOutBlock(string id, string name, params Variable[] variables)
    {
        if (variables.Any(v => v.VariableType != EVariableType.InOut))
            throw new ArgumentException("Invalid binding type!");
        var bInOut = new BasicBlockDef(id, name);
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

    public static BasicBlockDef CreateBlockSimple(
        string id, string name, string content, ERuntime runtime = ERuntime.Javascript,
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
            runtime: runtime,
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