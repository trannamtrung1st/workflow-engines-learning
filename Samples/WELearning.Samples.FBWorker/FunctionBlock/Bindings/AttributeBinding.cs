using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Samples.FBWorker.FunctionBlock.Bindings.Abstracts;
using WELearning.Samples.FBWorker.FunctionBlock.ValueObjects;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.FBWorker.FunctionBlock.Bindings;

public class AttributeBinding : ReadWriteBinding, IAssetAttributeBinding
{
    private new readonly AttributeValueObject _valueObject;
    private readonly IAssetService _assetService;

    public AttributeBinding(string name, AttributeValueObject valueObject, IAssetService assetService) : base(name, valueObject)
    {
        _valueObject = valueObject;
        _assetService = assetService;
    }

    public string AttributeName => _valueObject.AttributeName;
    public AttributeSnapshot Snapshot => _valueObject.Snapshot;

    public async Task<MetricSeries> LastSeriesBefore(DateTime beforeTime)
    {
        var series = await _assetService.LastSeriesBefore(
            assetId: Snapshot.AssetId,
            attributeName: Snapshot.AttributeName,
            beforeTime);
        return series;
    }
}

public class ReadAttributeBinding : IReadBinding, IAssetAttributeBinding
{
    private readonly AttributeBinding _attributeBinding;
    public ReadAttributeBinding(string name, AttributeValueObject valueObject, IAssetService assetService)
    {
        _attributeBinding = new AttributeBinding(name, valueObject, assetService);
    }

    public object Value => _attributeBinding.Value;
    public bool IsNumeric => _attributeBinding.IsNumeric;
    public string Name => _attributeBinding.Name;
    public bool ValueSet => _attributeBinding.ValueSet;
    public string AttributeName => _attributeBinding.AttributeName;
    public AttributeSnapshot Snapshot => _attributeBinding.Snapshot;

    public double AsDouble() => _attributeBinding.AsDouble();
    public int AsInt() => _attributeBinding.AsInt();
    public Task<MetricSeries> LastSeriesBefore(DateTime beforeTime) => _attributeBinding.LastSeriesBefore(beforeTime);
}