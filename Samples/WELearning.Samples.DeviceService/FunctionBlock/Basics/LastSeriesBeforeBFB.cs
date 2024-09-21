using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Helpers;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.Samples.DeviceService.FunctionBlock.Constants;

namespace WELearning.Samples.DeviceService.FunctionBlock.Basics;

public static class LastSeriesBeforeBFB
{
    public static BasicBlockDef Build()
    {
        return BlockHelper.CreateBlockSimple(id: "LastSeriesBefore", name: "Get last series of attribute before given time",
            content: @"
            if (!BeforeTime)
                throw new Error('BeforeTime cannot be null!');
            const series = await Attribute.LastSeriesBefore(BeforeTime);
            Result = series;
            ", runtime: ERuntime.Javascript, imports: null, importModuleRefs: null,
            signature: "LastSeriesBefore", exported: true,
            new Variable("Attribute", EDataType.Reference, EVariableType.Input, objectType: BindingNames.AttributeBinding),
            new Variable("BeforeTime", EDataType.DateTime, EVariableType.Input),
            new Variable("Result", EDataType.Object, EVariableType.Output));
    }
}