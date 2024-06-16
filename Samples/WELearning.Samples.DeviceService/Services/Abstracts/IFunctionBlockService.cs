using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IFunctionBlockService
{
    Task<BlockDefinitions> GetBlockDefinitions(string demoBlockId);
}
