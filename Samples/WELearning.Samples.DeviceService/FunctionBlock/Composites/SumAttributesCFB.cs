using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.Constants;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.Samples.DeviceService.FunctionBlock.Constants;
using WELearning.Core.FunctionBlocks.Helpers;

namespace WELearning.Samples.DeviceService.FunctionBlock.Composites;

public static class SumAttributesCFB
{
    const string IOBoundId = "io";
    const string CpuBoundId = "cpu";

    public static CompositeBlockDef BuildIOBound(string bLastSeriesBeforeId, string bAddId, out BasicBlockDef bSumDef, out BasicBlockDef bInputsDef, out BasicBlockDef bOutputsDef)
    {
        var cfb = new CompositeBlockDef(
            id: IOBoundId,
            name: "Produce attributes sum and previous sum (IO bound)");

        var attrType = BindingNames.AttributeBinding;
        var iAttr1 = new Variable("Attr1", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var iAttr2 = new Variable("Attr2", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var oSum = new Variable("AttrSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        var oPrevSum = new Variable("AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        cfb.Variables = new Variable[] { iAttr1, iAttr2, oSum, oPrevSum };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iAttr1.Name, iAttr2.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oSum.Name, oPrevSum.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        var bAddDef = PredefinedBFBs.AddJs;
        bSumDef = BlockHelper.CreateBlockSimple(
            id: $"{IOBoundId}_Sum",
            name: "Sum",
            content:
            @$"
            if (!Attr1.Value || !Attr2.Value) {{
                FB.Terminate();
            }}
            AttrSum = Attr1.Value + Attr2.Value;
            ",
            runtime: ERuntime.Javascript, imports: null, importModuleRefs: null, signature: null, exported: false,
            new Variable("Attr1", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("Attr2", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("AttrSum", EDataType.Reference, EVariableType.Output, objectType: attrType)
        );
        var bSum = new BlockInstance(bSumDef.Id, id: $"Sum-{Guid.NewGuid()}");

        var bLastSeries1 = new BlockInstance(bLastSeriesBeforeId, id: $"LastSeries1-{Guid.NewGuid()}");
        var bLastSeries2 = new BlockInstance(bLastSeriesBeforeId, id: $"LastSeries2-{Guid.NewGuid()}");
        var bAdd = new BlockInstance(bAddId, id: $"Add-{Guid.NewGuid()}");

        bInputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Attr1", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "Attr2", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: $"Inputs-{Guid.NewGuid()}");

        bOutputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "AttrSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: $"Outputs-{Guid.NewGuid()}");

        {
            var blocks = new List<BlockInstance> { bSum, bLastSeries1, bLastSeries2, bAdd, bInputs, bOutputs };
            cfb.Blocks = blocks;
        }

        {
            var eventConnections = new List<BlockEventConnection>();

            // [NOTE] CFB input events
            eventConnections.Add(new(blockId: bInputs.Id, eventName: "Trigger")
            {
                SourceEventName = "Trigger"
            });

            eventConnections.Add(new(blockId: bSum.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bLastSeries1.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bLastSeries2.Id, eventName: "Trigger")
            {
                SourceBlockId = bInputs.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bAdd.Id, eventName: "Trigger")
            {
                SourceBlockId = bLastSeries1.Id,
                SourceEventName = "Completed"
            });
            eventConnections.Add(new(blockId: bOutputs.Id, eventName: "Trigger")
            {
                SourceBlockId = bAdd.Id,
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

            dataConnections.Add(new(blockId: bLastSeries1.Id, variableName: "Attribute", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr1"
            });
            dataConnections.Add(new(blockId: bLastSeries1.Id, variableName: "BeforeTime", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr1",
                SourceProperty = "Timestamp"
            });
            dataConnections.Add(new(blockId: bLastSeries2.Id, variableName: "Attribute", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr2"
            });
            dataConnections.Add(new(blockId: bLastSeries2.Id, variableName: "BeforeTime", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr2",
                SourceProperty = "Timestamp"
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "X", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bLastSeries1.Id,
                SourceVariableName = "Result",
                Preprocessing = Function.CreateRawExpression(content: "THIS?.Value", runtime: ERuntime.Javascript)
            });
            dataConnections.Add(new(blockId: bAdd.Id, variableName: "Y", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bLastSeries2.Id,
                SourceVariableName = "Result",
                Preprocessing = Function.CreateRawExpression(content: "THIS?.Value", runtime: ERuntime.Javascript)
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "AttrSum", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bSum.Id,
                SourceVariableName = "AttrSum"
            });
            dataConnections.Add(new(blockId: bOutputs.Id, variableName: "AttrPrevSum", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bAdd.Id,
                SourceVariableName = "Result"
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
            references.Add(new(blockId: bSum.Id, variableName: "Attr1", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr1"
            });
            references.Add(new(blockId: bSum.Id, variableName: "Attr2", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr2"
            });
            references.Add(new(blockId: bSum.Id, variableName: "AttrSum", displayName: null, bindingType: EBindingType.Output)
            {
                SourceBlockId = bOutputs.Id,
                SourceVariableName = "AttrSum"
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

    public static CompositeBlockDef BuildCpuBound(out BasicBlockDef bMainDef, out BasicBlockDef bInputsDef, out BasicBlockDef bOutputsDef)
    {
        var cfb = new CompositeBlockDef(
            id: CpuBoundId,
            name: "Produce attributes sum and previous sum (CPU bound)");

        var attrType = BindingNames.AttributeBinding;
        var iAttr1 = new Variable("Attr1", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var iAttr2 = new Variable("Attr2", dataType: EDataType.Reference, variableType: EVariableType.Input, objectType: attrType);
        var oSum = new Variable("AttrSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        var oPrevSum = new Variable("AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.Output, objectType: attrType);
        cfb.Variables = new Variable[] { iAttr1, iAttr2, oSum, oPrevSum };

        var eTrigger = new BlockEvent(isInput: true, name: "Trigger", variableNames: new[] { iAttr1.Name, iAttr2.Name });
        var eCompleted = new BlockEvent(isInput: false, name: "Completed", variableNames: new[] { oSum.Name, oPrevSum.Name });
        cfb.Events = new[] { eTrigger, eCompleted };
        cfb.DefaultTriggerEvent = eTrigger.Name;

        bMainDef = BlockHelper.CreateBlockSimple(
            id: $"{CpuBoundId}_Main",
            name: "Main function",
            content:
@$"if (!Attr1.Value || !Attr2.Value) {{
    FB.Terminate();
}}

AttrPrevSum = AttrSum.Value;
AttrSum = Attr1.Value + Attr2.Value;",
            runtime: ERuntime.Javascript, imports: null, importModuleRefs: null, signature: null, exported: false,
            new Variable("Attr1", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("Attr2", EDataType.Reference, EVariableType.Input, objectType: attrType),
            new Variable("AttrSum", EDataType.Reference, EVariableType.Output, objectType: attrType),
            new Variable("AttrPrevSum", EDataType.Reference, EVariableType.Output, objectType: attrType)
        );
        var bMain = new BlockInstance(bMainDef.Id, id: $"Main-{Guid.NewGuid()}");

        bInputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "Attr1", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "Attr2", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bInputs = new BlockInstance(definitionId: bInputsDef.Id, id: $"Inputs-{Guid.NewGuid()}");

        bOutputsDef = BlockHelper.CreateInOutBlock(
            new Variable(name: "AttrSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType),
            new Variable(name: "AttrPrevSum", dataType: EDataType.Reference, variableType: EVariableType.InOut, objectType: attrType)
        );
        var bOutputs = new BlockInstance(definitionId: bOutputsDef.Id, id: $"Outputs-{Guid.NewGuid()}");

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
                SourceBlockId = bInputs.Id,
                SourceVariableName = "Attr1"
            });
            references.Add(new(blockId: bMain.Id, variableName: "Attr2", displayName: null, bindingType: EBindingType.Input)
            {
                SourceBlockId = bInputs.Id,
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