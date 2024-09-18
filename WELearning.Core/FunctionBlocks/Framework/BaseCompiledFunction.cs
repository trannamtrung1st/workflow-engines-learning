using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework;

public abstract class BaseCompiledFunction<TFunctionFramework> : IExecutable<object, BlockGlobalObject<TFunctionFramework>>
    where TFunctionFramework : IFunctionFramework
{
    protected TFunctionFramework FB;
    protected IReadOnlyDictionary<string, IReadBinding> IN;
    protected IReadOnlyDictionary<string, IWriteBinding> OUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INOUT;
    protected IReadOnlyDictionary<string, IReadWriteBinding> INTERNAL;
    protected IOutputEventPublisher EVENTS;

    public Task<object> Execute(BlockGlobalObject<TFunctionFramework> global, CancellationToken cancellationToken)
    {
        FB = global.FB;
        EVENTS = global.EVENTS;
        return Handle(cancellationToken);
    }

    public abstract Task<object> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var frameworkTypeName = typeof(TFunctionFramework).FullName;
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

public abstract class BaseCompiledFunction<TReturn, TFunctionFramework> : IExecutable<TReturn, BlockGlobalObject<TFunctionFramework>>
    where TFunctionFramework : IFunctionFramework
{
    protected TFunctionFramework FB;
    protected IOutputEventPublisher EVENTS;

    public Task<TReturn> Execute(BlockGlobalObject<TFunctionFramework> global, CancellationToken cancellationToken)
    {
        FB = global.FB;
        EVENTS = global.EVENTS;
        return Handle(cancellationToken);
    }

    public abstract Task<TReturn> Handle(CancellationToken cancellationToken);

    public static string WrapScript(string script)
    {
        var returnTypeName = typeof(TReturn).FullName;
        var frameworkTypeName = typeof(TFunctionFramework).FullName;
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
