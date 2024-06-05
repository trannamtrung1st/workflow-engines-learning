using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution.Constants;

public static class JsEngineConstants
{
    public const string ExportedFunctionName = nameof(IExecutable<object>.Execute);
    public const string DefaultArgumentsName = "args";
}