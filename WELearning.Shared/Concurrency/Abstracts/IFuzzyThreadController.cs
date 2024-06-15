namespace WELearning.Shared.Concurrency.Abstracts;

public interface IFuzzyThreadController
{
    int GetThreadScale(double cpu, double memory, double ideal, int factor = 10);
}