using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BaseBlockFrameworkFunction : IExecutable<BlockGlobalObject>
{
    protected IBlockFrameworkInstance FB;

    public Task Execute(BlockGlobalObject global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        return @$"
public class Function : BaseBlockFrameworkFunction
{{
    public override async Task Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}

public abstract class BaseBlockFrameworkFunction<TReturn> : IExecutable<TReturn, BlockGlobalObject>
{
    protected IBlockFrameworkInstance FB;

    public Task<TReturn> Execute(BlockGlobalObject global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task<TReturn> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var returnTypeName = typeof(TReturn).FullName;
        return @$"
public class Function : BaseBlockFrameworkFunction<{returnTypeName}>
{{
    public override async Task<{returnTypeName}> Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}
