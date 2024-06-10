namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IMonitoring
{
    void Capture(string category, int count);
    int GetRate(string category);
    void PrintAll();
    void StartReport();
}
