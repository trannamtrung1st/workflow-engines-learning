using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BaseBlockFrameworkFunction<TFrameworkInstance> : IExecutable<BlockGlobalObject<TFrameworkInstance>>
{
    protected TFrameworkInstance FB;

    public Task Execute(BlockGlobalObject<TFrameworkInstance> global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var frameworkTypeName = typeof(TFrameworkInstance).FullName;
        return @$"
public class Function : BaseBlockFrameworkFunction<{frameworkTypeName}>
{{
    public override async Task Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}

public abstract class BaseBlockFrameworkFunction<TReturn, TFrameworkInstance> : IExecutable<TReturn, BlockGlobalObject<TFrameworkInstance>>
{
    protected TFrameworkInstance FB;

    public Task<TReturn> Execute(BlockGlobalObject<TFrameworkInstance> global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task<TReturn> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var returnTypeName = typeof(TReturn).FullName;
        var frameworkTypeName = typeof(TFrameworkInstance).FullName;
        return @$"
public class Function : BaseBlockFrameworkFunction<{returnTypeName}, {frameworkTypeName}>
{{
    public override async Task<{returnTypeName}> Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}
