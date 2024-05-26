using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BlockFramework<TFrameworkInstance> : IBlockFramework<TFrameworkInstance>
{
    protected readonly BlockExecutionControl _control;
    protected readonly ConcurrentDictionary<string, BlockBinding> _blockBindings;

    public BlockFramework(BlockExecutionControl control)
    {
        _control = control;
        _blockBindings = new ConcurrentDictionary<string, BlockBinding>();
    }

    public IBlockBinding Get(string name)
        => _blockBindings.GetOrAdd(name, (key) => new BlockBinding(key, control: _control));

    public double GetDouble(string name)
    {
        var binding = Get(name);
        return binding.GetDouble();
    }

    public Task Set(string name, object value)
    {
        var binding = Get(name);
        return binding.Set(value);
    }

    public abstract TFrameworkInstance CreateInstance();
}

public class BlockFramework : BlockFramework<IBlockFrameworkInstance>
{
    public BlockFramework(BlockExecutionControl control) : base(control)
    {
    }

    public override IBlockFrameworkInstance CreateInstance() => new BlockFrameworkInstance<BlockFramework, IBlockFrameworkInstance>(this);
}
