using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static string WrapModuleFunction(
        string script, string topStatements = null,
        string bottomStatements = null,
        string functionName = nameof(IExecutable<object>.Execute),
        string globalVar = "_FB_", IEnumerable<string> inputVariables = null
    )
    {
        var argumentsStr = inputVariables?.Any() == true ? string.Join(',', inputVariables) : globalVar;
        return @$"
        {topStatements}
        export async function {functionName}({argumentsStr}) {{
            {script}
        }}
        {bottomStatements}
        ";
    }
}
