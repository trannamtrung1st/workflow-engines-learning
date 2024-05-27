using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BaseCompiledFunction<TFramework> : IExecutable<BlockGlobalObject<TFramework>>
{
    protected TFramework FB;

    public Task Execute(BlockGlobalObject<TFramework> global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var frameworkTypeName = typeof(TFramework).FullName;
        return @$"
public class Function : BaseCompiledFunction<{frameworkTypeName}>
{{
    public override async Task Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}

public abstract class BaseCompiledFunction<TReturn, TFramework> : IExecutable<TReturn, BlockGlobalObject<TFramework>>
{
    protected TFramework FB;

    public Task<TReturn> Execute(BlockGlobalObject<TFramework> global, CancellationToken cancellationToken = default)
    {
        FB = global.FB;
        return Handle(cancellationToken);
    }

    public abstract Task<TReturn> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var returnTypeName = typeof(TReturn).FullName;
        var frameworkTypeName = typeof(TFramework).FullName;
        return @$"
public class Function : BaseCompiledFunction<{returnTypeName}, {frameworkTypeName}>
{{
    public override async Task<{returnTypeName}> Handle(CancellationToken cancellationToken)
    {{
        {script}
    }}
}}";
    }
}
