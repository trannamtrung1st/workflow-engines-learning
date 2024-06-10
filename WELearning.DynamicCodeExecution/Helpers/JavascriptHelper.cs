using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class JavascriptHelper
{
    public static (string Content, int LineStart, int LineEnd, int IndexStart, int IndexEnd) WrapModuleFunction(
        string script, bool async, string returnStatements = null, string topStatements = null,
        string bottomStatements = null, string functionName = JsEngineConstants.ExportedFunctionName,
        IEnumerable<string> flattenArguments = null, IEnumerable<string> flattenOutputs = null
    )
    {
        var argumentsStr = flattenArguments?.Any() == true ? string.Join(',', flattenArguments) : null;
        var asyncStr = async ? "async " : null;
        returnStatements ??= GetPreprocessOutputContent(flattenOutputs);

        const string Inputs = JsEngineConstants.InputsArgument;
        const string Outputs = JsEngineConstants.OutputsArgument;
        const string Wrap = JsEngineConstants.WrapFunction;
        const string WrapResult = JsEngineConstants.WrapResultVariable;

        var topContent =
@$"{topStatements}
export {asyncStr}function {functionName}({Inputs}, {Outputs}) {{
    let {{{argumentsStr}}} = {{...{Inputs}, ...{Outputs}}};
    {asyncStr}function {Wrap}() {{
";

        var bottomContent =
@$" }}
    const {WrapResult} = {(async ? "await " : null)}{Wrap}();
    {returnStatements}
}}
{bottomStatements}";

        var finalContent =
@$"{topContent}
{script}
{bottomContent}";

        var scriptLineCount = script.NewLineCount();
        var topLineCount = topContent.NewLineCount();
        var topLength = topContent.Length;
        var scriptLength = script.Length;
        return (finalContent, topLineCount + 1, topLineCount + scriptLineCount, topLength + 1, topLength + scriptLength);
    }

    private static string GetPreprocessOutputContent(IEnumerable<string> flattenOutputs)
    {
        const string WrapResult = JsEngineConstants.WrapResultVariable;
        const string Out = JsEngineConstants.EngineOutVariable;
        var flattenOutputsStr = flattenOutputs?.Any() == true ?
@$"if ({WrapResult} !== undefined) return {WrapResult};
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
