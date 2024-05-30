namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockBinding
{
    string Name { get; }
    bool ValueSet { get; }
}

public interface IInputBinding : IBlockBinding
{
    object Value { get; }
    double ToDouble();
    int ToInt();
    bool IsNumeric { get; }
    bool Is<T>();
}

public interface IOutputBinding : IBlockBinding
{
    Task Set(object value);
}

public interface IReadWriteBinding : IInputBinding, IOutputBinding
{
}