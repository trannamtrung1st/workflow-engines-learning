using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IValueObject
{
    event EventHandler ValueSetEvent;

    Variable Variable { get; }
    bool ValueChanged { get; }
    bool ValueSet { get; }
    object Value { get; set; }
    bool TempValueSet { get; }
    object TempValue { get; set; }
    bool IsNumeric { get; }
    bool IsRaw { get; }
    object GetProperty(string name);
    void TryCastAndSet(object value);
    object TryCast(object value);
    void TryCommit();
    void WaitValueSet(CancellationToken cancellationToken);
    bool RegisterTempValueSet(Func<Task> callback);
    IValueObject CloneFor(Variable variable);
}
