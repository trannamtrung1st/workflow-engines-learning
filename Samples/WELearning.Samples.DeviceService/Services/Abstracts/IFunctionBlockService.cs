
using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Samples.DeviceService.Services.Abstracts;

public interface IFunctionBlockService
{
    Task<CompositeBlockDef> BuildBlock(string demoBlockId);
}
