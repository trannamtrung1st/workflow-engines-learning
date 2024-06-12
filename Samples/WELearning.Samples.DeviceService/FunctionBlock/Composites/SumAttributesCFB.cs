using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.ConsoleApp.Testing.Framework.Bindings;
using WELearning.Core.FunctionBlocks;

namespace WELearning.Samples.DeviceService.FunctionBlock.Composites;

public static class SumAttributesCFB
{
    public const string CfbId = "SumAttributes";

    public static CompositeBlockDef Build(string lastSeriesBeforeBfbDefId, out BasicBlockDef bMainDef, out BasicBlockDef bInputsDef, out BasicBlockDef bOutputsDef)
    {
        var cfb = new CompositeBlockDef(
            id: CfbId,
            name: "Produce sum of 2 attributes and record their previous sum");

        var attrType = nameof(AttributeBinding);
        var iAttr1 = new Variable("Attr1", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var iAttr2 = new Variable("Attr2", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var oSum = new Variable("AttrSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        var oPrevSum = new Variable("AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        cfb.Variables = new Variable[] { iAttr1, iAttr2, oSum, oPrevSum };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iAttr1.Name, iAttr2.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oSum.Name, oPrevSum.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        // [TODO] demo async blocks
        bMainDef = PredefinedBFBs.CreateBlockSimple(
            id: $"{CfbId}_Main",
            name: "Main function",
            content:
            @$"
            if (!Attr1.Value || !Attr2.Value) {{
                AttrSum = null; AttrPrevSum = null;
                return;
            }}
            
            const prevSumFromAttr = AttrSum.Value;
            AttrSum = Attr1.Value + Attr2.Value;
            // await AttrSum.Write(Attr1.Value + Attr2.Value);
            
            const {{ Timestamp }} = Attr1.Snapshot;
            const lastAttr1 = await LastSeriesBefore({{ Attribute: Attr1, BeforeTime: Timestamp }}, {{ Result: null }});
            const lastAttr2 = await LastSeriesBefore({{ Attribute: Attr2, BeforeTime: Timestamp }}, {{ Result: null }});
            AttrPrevSum = null;
            if (lastAttr1?.Result && lastAttr2?.Result) {{
                const prevSumValue = lastAttr1.Result.Value + lastAttr2.Result.Value;
                FB.LogTrace('Sum:', prevSumFromAttr, prevSumValue);
                AttrPrevSum = prevSumValue;
                // await AttrPrevSum.Write(prevSumValue);
            }}",
            imports: new[] { $"import {{ LastSeriesBefore }} from '{FunctionDefaults.ModuleFunctions}'" },
            importBlockIds: new[] { lastSeriesBeforeBfbDefId }, signature: null, exported: false,
            new Variable("Attr1", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("Attr2", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("AttrSum", EDataType.Reference, EVariableType.Output, objectType: attrType),
            new Variable("AttrPrevSum", EDataType.Reference, EVariableType.Output, objectType: attrType)
        );
        var bMain = new BlockInstance(bMainDef.Id, id: "Main");

        bInputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "Attr1", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "Attr2", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: "Inputs");

        bOutputsDef = PredefinedBFBs.CreateInOutBlock(
            new Variable(name: "AttrSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: "Outputs");

        {
            var blocks = new List<BlockInstance> { bMain, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bMain.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bMain.Id,
                SourceEventName = "Completed"
            });

            // [NOTE] CFB output events
            eventConnections.Add(new(blockId: null, eventName: "Completed")
            {
                SourceBlockId = bOutputs.Id,
                SourceEventName = "Completed"
            });

            cfb.EventConnections = eventConnections;
        }

        {
            var dataConnections = new List<BlockConnection>();

            dataConnections.Add(new(blockId: bMain.Id, variableName: "Attr1", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr1"
            });
            dataConnections.Add(new(blockId: bMain.Id, variableName: "Attr2", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr2"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "AttrSum", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMain.Id,
                SourceVariableName = "AttrSum"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "AttrPrevSum", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bMain.Id,
                SourceVariableName = "AttrPrevSum"
            });

            // [NOTE] CFB output data
            foreach (var variable in cfb.Variables.Where(v => v.VariableType == EVariableType.Output || v.VariableType == EVariableType.InOut))
            {
                dataConnections.Add(new(blockId: null, variableName: variable.Name, displayName: null, bindingType: EBindingType.Output)
                {
                    SourceBlockId = bOutputs.Id,
                    SourceVariableName = variable.Name
                });
            }

            cfb.DataConnections = dataConnections;
        }

        {
            var references = new List<BlockConnection>();
            references.Add(new(blockId: bInputs.Id, variableName: "Attr1", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Attr1"
            });
            references.Add(new(blockId: bInputs.Id, variableName: "Attr2", displayName: null, bindingType: EBindingType.Input)
            {
                SourceVariableName = "Attr2"
            });
            references.Add(new(blockId: bMain.Id, variableName: "Attr1", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = "Inputs",
                SourceVariableName = "Attr1"
            });
            references.Add(new(blockId: bMain.Id, variableName: "Attr2", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = "Inputs",
                SourceVariableName = "Attr2"
            });
            references.Add(new(blockId: bMain.Id, variableName: "AttrSum", displayName: null, bindingType: EBindingType.Output)
            {
                SourceBlockId = bOutputs.Id,
                SourceVariableName = "AttrSum"
            });
            references.Add(new(blockId: bMain.Id, variableName: "AttrPrevSum", displayName: null, bindingType: EBindingType.Output)
            {
                SourceBlockId = bOutputs.Id,
                SourceVariableName = "AttrPrevSum"
            });
            references.Add(new(blockId: bOutputs.Id, variableName: "AttrSum", displayName: null, bindingType: EBindingType.Output)
            {
                SourceVariableName = "AttrSum"
            });
            references.Add(new(blockId: bOutputs.Id, variableName: "AttrPrevSum", displayName: null, bindingType: EBindingType.Output)
            {
                SourceVariableName = "AttrPrevSum"
            });
            cfb.References = references;
        }

        return cfb;
    }

}