using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BaseCompiledFunction<TBlockFramework> : IExecutable<object, BlockGlobalObject<TBlockFramework>>
{
    protected TBlockFramework FB;
    protected IReadOnlyDictionary<string, IReadBinding> IN;
    protected IReadOnlyDictionary<string, IWriteBinding> OUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INOUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INTERNAL;
    protected IOutputEventPublisher EVENTS;

    public Task<object> Execute(BlockGlobalObject<TBlockFramework> global, CancellationToken cancellationToken)
    {
        FB = global.FB;
        IN = global.IN;
        OUT = global.OUT;
        INOUT = global.INOUT;
        INTERNAL = global.INTERNAL;
        EVENTS = global.EVENTS;
        return Handle(cancellationToken);
    }

    public abstract Task<object> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var frameworkTypeName = typeof(TBlockFramework).FullName;
        return @$"
public class Function : BaseCompiledFunction<{frameworkTypeName}>
{{
    public override async Task<object> Handle(CancellationToken cancellationToken)
    {{
        {script}
        return null;
    }}
}}";
    }
}

public abstract class BaseCompiledFunction<TReturn, TBlockFramework> : IExecutable<TReturn, BlockGlobalObject<TBlockFramework>>
{
    protected TBlockFramework FB;
    protected IReadOnlyDictionary<string, IReadBinding> IN;
    protected IReadOnlyDictionary<string, IWriteBinding> OUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INOUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INTERNAL;
    protected IOutputEventPublisher EVENTS;

    public Task<TReturn> Execute(BlockGlobalObject<TBlockFramework> global, CancellationToken cancellationToken)
    {
        FB = global.FB;
        IN = global.IN;
        OUT = global.OUT;
        INOUT = global.INOUT;
        INTERNAL = global.INTERNAL;
        EVENTS = global.EVENTS;
        return Handle(cancellationToken);
    }

    public abstract Task<TReturn> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var returnTypeName = typeof(TReturn).FullName;
        var frameworkTypeName = typeof(TBlockFramework).FullName;
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
