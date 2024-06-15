namespace WELearning.Shared.Concurrency.Abstracts;

public interface IFuzzyThreadController
{
    int GetThreadScale(double cpu, double memory, double ideal, int factor = 10, double incFactor = 1, double decFactor = 2);
}