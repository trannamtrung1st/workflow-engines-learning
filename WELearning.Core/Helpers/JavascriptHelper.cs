namespace WELearning.Core.Helpers;

public static class JavascriptHelper
{
    public static string WrapTopLevelAsyncCall(string script)
    {
        return @$"
        async function executeScript() {{
            {script}
        }}
        executeScript();
        ";
    }
}
