using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.ConsoleApp.Testing.CompositeBlocks;

public static class SimpleCFB
{
    public static CompositeBlockDef Build(BasicBlockDef bSimpleDef)
    {
        var cfb = new CompositeBlockDef(id: "Simple", name: "Simple CFB");
        cfb.Variables = Array.Empty<Variable>();

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: Array.Empty<string>());
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: Array.Empty<string>());
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bSimple = new BlockInstance(bSimpleDef.Id);
        cfb.Blocks = new[] { bSimple };

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bSimple.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            // [NOTE] CFB output events
            eventConnections.Add(new(blockId: null, eventName: "Completed")
            {
                SourceBlockId = bSimple.Id,
                SourceEventName = "Completed"
            });

            cfb.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockConnection>();
            cfb.DataConnections = dataConnections;
        }

        cfb.MapDefinitions(new[] { bSimpleDef });
        return cfb;
    }
}