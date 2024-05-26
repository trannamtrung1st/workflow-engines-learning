using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public class BlockFramework : IBlockFramework
{
    private readonly BlockExecutionControl _control;
    private readonly ConcurrentDictionary<string, BlockBinding> _blockBindings;

    public BlockFramework(BlockExecutionControl control)
    {
        _control = control;
        _blockBindings = new ConcurrentDictionary<string, BlockBinding>();
    }

    public IBlockBinding Get(string name)
    {
        return _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, control: _control));
    }

    public Task Set(string name, object value)
    {
        var binding = Get(name);
        return binding.Set(value);
    }

    public IBlockFrameworkInstance CreateInstance()
    {
        return new BlockFrameworkInstance(blockFramework: this);
    }
}
