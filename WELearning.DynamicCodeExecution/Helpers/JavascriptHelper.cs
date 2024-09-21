using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static UserContentInfo WrapModuleFunction(
        string script, bool async, string returnStatements = null, string topStatements = null,
        string bottomStatements = null, string functionName = JsEngineConstants.ExportedFunctionName,
        bool isScript = false, IEnumerable<string> flattenArguments = null, IEnumerable<string> flattenOutputs = null
    )
    {
        var argumentsStr = flattenArguments?.Any() == true ? string.Join(',', flattenArguments) : null;
        var asyncStr = async ? "async " : null;
        var exportStr = isScript ? null : "export ";
        returnStatements ??= GetPreprocessOutputContent(flattenOutputs);

        const string Inputs = JsEngineConstants.InputsArgument;
        const string Outputs = JsEngineConstants.OutputsArgument;
        const string Main = JsEngineConstants.MainFunction;
        const string MainResult = JsEngineConstants.MainResultVariable;

        var topContent =
@$"{topStatements}
{exportStr}{asyncStr}function {functionName}({Inputs}, {Outputs}) {{
    let {{{argumentsStr}}} = {{...{Inputs}, ...{Outputs}}};
    {asyncStr}function {Main}() {{
";

        var bottomContent =
@$" }}
    const {MainResult} = {(async ? "await " : null)}{Main}();
    {returnStatements}
}}
{bottomStatements}";

        var finalContent =
@$"{topContent}
{script}
{bottomContent}";

        var scriptLineCount = script.BreakLines().Length;
        var topLineCount = topContent.BreakLines().Length;
        var lines = finalContent.BreakLines();
        var topLength = topContent.Length;
        var scriptLength = script.Length;
        return (finalContent, lines, topLineCount + 1, topLineCount + scriptLineCount, topLength + 1, topLength + 1 + scriptLength);
    }

    private static string GetPreprocessOutputContent(IEnumerable<string> flattenOutputs)
    {
        const string MainResult = JsEngineConstants.MainResultVariable;
        const string Out = JsEngineConstants.EngineOutVariable;
        var flattenOutputsStr = flattenOutputs?.Any() == true ?
@$"if ({MainResult} !== undefined) return {MainResult};
const {Out} = {{{string.Join(',', flattenOutputs)}}};
Object.keys({Out}).forEach(key => {{
    if ({Out}[key] === undefined) {{
        delete {Out}[key];
    }}
}});
return {Out};" : string.Empty;
        return flattenOutputsStr;
    }
}
