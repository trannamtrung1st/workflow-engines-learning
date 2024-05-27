namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockBinding
{
    string Name { get; }
    object Value { get; }
    bool ValueSet { get; }
    bool Is<T>();
    bool IsNumeric { get; }
    Task Set(object value);
    double GetDouble();
}