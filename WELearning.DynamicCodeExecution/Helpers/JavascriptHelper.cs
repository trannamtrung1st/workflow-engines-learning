using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Extensions;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static (string Content, int LineStart, int LineEnd, int IndexStart, int IndexEnd) WrapModuleFunction(
        string script, string returnStatements = null, string topStatements = null,
        string bottomStatements = null,
        string functionName = nameof(IExecutable<object>.Execute),
        string globalVar = "_FB_", IEnumerable<string> inputVariables = null
    )
    {
        string nl = Environment.NewLine;
        var argumentsStr = inputVariables?.Any() == true ? string.Join(',', inputVariables) : globalVar;
        var topContent = $"{topStatements}{nl}export async function {functionName}({argumentsStr}) {{";
        var bottomContent = $"}}{nl}{bottomStatements}";
        var finalContent = $"{topContent}\n{script}\n{returnStatements}\n{bottomContent}";
        var scriptLineCount = script.NewLineCount();
        var topLineCount = topContent.NewLineCount();
        var topLength = topContent.Length;
        var scriptLength = script.Length;
        return (finalContent, topLineCount + 1, topLineCount + scriptLineCount, topLength + 1, topLength + scriptLength);
    }
}
