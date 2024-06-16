using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Samples.Shared.Models;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockService : IFunctionBlockService
{
    private readonly DataStore _dataStore;

    public FunctionBlockService(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public async Task<BlockDefinitions> GetBlockDefinitions(string demoBlockId)
    {
        var cfbDef = await _dataStore.GetCfbDefinition(demoBlockId);

        var usingBfbDefIds = cfbDef.Blocks.Select(b => b.DefinitionId);
        var usingBfbDefs = await _dataStore.GetBfbDefinitions(usingBfbDefIds);

        var importBfbDefIds = usingBfbDefs
            .Where(b => b.ImportBlockIds?.Any() == true)
            .SelectMany(b => b.ImportBlockIds);
        var importBfbDefs = await _dataStore.GetBfbDefinitions(importBfbDefIds);

        return new BlockDefinitions
        {
            Cfb = cfbDef,
            Bfbs = usingBfbDefs.Concat(importBfbDefs)
        };
    }
}