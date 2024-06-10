using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.ConsoleApp.Testing.Framework;

public class AppBlockFrameworkFactory : BlockFrameworkFactory
{
    private readonly DataStore _dataStore;
    public AppBlockFrameworkFactory(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public override IBlockFramework Create(IExecutionControl control) => new AppBlockFramework(control, _dataStore);
}