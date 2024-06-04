namespace WELearning.Shared.Concurrency.Abstracts;

public interface ILock : IDisposable
{
    string Key { get; }
}