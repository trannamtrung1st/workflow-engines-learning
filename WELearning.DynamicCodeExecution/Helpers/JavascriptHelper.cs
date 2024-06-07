using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static (string Content, int LineStart, int LineEnd, int IndexStart, int IndexEnd) WrapModuleFunction(
        string script, bool async, string returnStatements = null, string topStatements = null,
        string bottomStatements = null, string functionName = JsEngineConstants.ExportedFunctionName,
        IEnumerable<string> flattenArguments = null
    )
    {
        string nl = Environment.NewLine;
        var argumentsStr = flattenArguments?.Any() == true ? string.Join(',', flattenArguments) : null;
        var topContent = $"{topStatements}{nl}export {(async ? "async " : null)}function {functionName}(INPUTS, OUTPUTS) {{\n"
            + $"let {{{argumentsStr}}} = {{...INPUTS, ...OUTPUTS}};{nl}";
        var bottomContent = $"}}{nl}{bottomStatements}";
        var finalContent = $"{topContent}\n{script}\n{returnStatements}\n{bottomContent}";
        var scriptLineCount = script.NewLineCount();
        var topLineCount = topContent.NewLineCount();
        var topLength = topContent.Length;
        var scriptLength = script.Length;
        return (finalContent, topLineCount + 1, topLineCount + scriptLineCount, topLength + 1, topLength + scriptLength);
    }
}
