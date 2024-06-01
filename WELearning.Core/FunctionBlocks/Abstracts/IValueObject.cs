using WELearning.Core.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IValueObject
{
    Variable Variable { get; }
    bool ValueChanged { get; }
    bool ValueSet { get; }
    object Value { get; set; }
    bool TempValueSet { get; }
    object TempValue { get; set; }
    bool IsNumeric { get; }
    bool IsRaw { get; }
    double AsDouble();
    int AsInt();
    bool AsBool();
    object As(EDataType dataType);
    void TryCommit();
    void WaitValueSet(CancellationToken cancellationToken);
    IValueObject CloneFor(Variable variable);
}
