namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IBlockBinding
{
    string Name { get; }
    bool ValueSet { get; }
}

public interface IReadBinding : IBlockBinding
{
    object Value { get; }
    double AsDouble();
    int AsInt();
    bool IsNumeric { get; }
}

public interface IWriteBinding : IBlockBinding
{
    Task Write(object value);
}

public interface IReadWriteBinding : IReadBinding, IWriteBinding
{
}