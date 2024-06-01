using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.Helpers;

public static class JavascriptHelper
{
    public static string WrapModuleFunction(
        string script, string topStatements = null, string bottomStatements = null,
        string globalVar = "_FB_", string frameworkProperty = "FB", IEnumerable<string> inputVariables = null
    )
    {
        IEnumerable<string> finalArgs = new[] { globalVar, frameworkProperty };
        finalArgs = inputVariables != null ? finalArgs.Concat(inputVariables) : finalArgs;
        return @$"
        {topStatements}
        export async function {nameof(IExecutable<object>.Execute)}({string.Join(',', finalArgs)}) {{
            {script}
        }}
        {bottomStatements}
        ";
    }

    public static string[] GetInputVariableNames(IEnumerable<Variable> variables)
    {
        return variables.Where(v => v.CanInput()).Select(v => v.Name).ToArray();
    }
}
