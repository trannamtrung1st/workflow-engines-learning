using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static string WrapModuleFunction(
        string script, string topStatements = null, string bottomStatements = null,
        string globalVar = "_FB_", IEnumerable<string> inputVariables = null
    )
    {
        IEnumerable<string> finalArgs = new[] { globalVar };
        finalArgs = inputVariables != null ? finalArgs.Concat(inputVariables) : finalArgs;
        return @$"
        {topStatements}
        export async function {nameof(IExecutable<object>.Execute)}({string.Join(',', finalArgs)}) {{
            {script}
        }}
        {bottomStatements}
        ";
    }
}
