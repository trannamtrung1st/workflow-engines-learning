using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution.Constants;

public static class JsEngineConstants
{
    public const string ExportedFunctionName = nameof(IExecutable<object>.Execute);
    public const string MainFunction = "main";
    public const string InputsArgument = "INPUTS";
    public const string OutputsArgument = "OUTPUTS";
    public const string MainResultVariable = "MAIN_RESULT";
    public const string EngineOutVariable = "ENGINE_RESULT";
}
