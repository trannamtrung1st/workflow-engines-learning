using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Samples.FBWorker.FunctionBlock.Bindings;
using WELearning.Samples.FBWorker.FunctionBlock.ValueObjects;
using WELearning.Samples.FBWorker.Services.Abstracts;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceBlockFramework : BlockFramework
{
    private readonly IAssetService _assetService;
    public DeviceBlockFramework(IExecutionControl control, IAssetService assetService) : base(control)
    {
        _assetService = assetService;
    }

    public override object GetBindingFor(IValueObject valueObject)
    {
        if (valueObject is AttributeValueObject attributeValue)
        {
            var variable = attributeValue.Variable;
            switch (variable.VariableType)
            {
                case EVariableType.Input: return new ReadAttributeBinding(variable.Name, attributeValue, _assetService);
                default: return new AttributeBinding(variable.Name, attributeValue, _assetService);
            }
        }

        return base.GetBindingFor(valueObject);
    }
}
