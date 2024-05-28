using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.Helpers;

public static class JavascriptHelper
{
    public static string WrapModuleFunction(
        string script, string topStatements = null, string bottomStatements = null,
        string globalVarName = "_FB_"
    )
    {
        return @$"
        {topStatements}
        export async function {nameof(IExecutable<object>.Execute)}({globalVarName}) {{
            {script}
        }}
        {bottomStatements}
        ";
    }
}
