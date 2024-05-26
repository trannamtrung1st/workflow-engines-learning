namespace WELearning.DynamicCodeExecution.Abstracts;

public interface IExecutable<TArg>
{
    Task Execute(TArg arguments, CancellationToken cancellationToken = default);
}

public interface IExecutable<TReturn, TArg>
{
    Task<TReturn> Execute(TArg arguments, CancellationToken cancellationToken = default);
}