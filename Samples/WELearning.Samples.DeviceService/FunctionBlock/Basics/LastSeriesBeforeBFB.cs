using WELearning.ConsoleApp.Testing.Framework.Bindings;
using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Samples.DeviceService.FunctionBlock.Basics;

public static class LastSeriesBeforeBFB
{
    public static BasicBlockDef Build()
    {
        return PredefinedBFBs.CreateBlockSimple(id: "LastSeriesBefore", name: "Get last series of attribute before given time",
            content: @"
            if (!BeforeTime)
                throw new Error('BeforeTime cannot be null!');
            const series = await Attribute.LastSeriesBefore(BeforeTime);
            Result = series;
            ", imports: null, importBlockIds: null,
            signature: "LastSeriesBefore", exported: true,
            new Variable("Attribute", EDataType.Reference, EVariableType.Input, objectType: nameof(AttributeBinding)),
            new Variable("BeforeTime", EDataType.DateTime, EVariableType.Input),
            new Variable("Result", EDataType.Object, EVariableType.Output));
    }
}