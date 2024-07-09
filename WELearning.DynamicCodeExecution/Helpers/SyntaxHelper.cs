using System.Text.RegularExpressions;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class SyntaxHelper
{
    public static bool HasAsyncSyntax(string content) => content.Contains("await "); // [NOTE] just for convenience, should use other mechanism or manually set

    public static string CleanFunctionSignature(string functionName)
    {
        if (string.IsNullOrEmpty(functionName))
        {
            return string.Empty;
        }

        // Remove invalid characters
        string cleanedName = Regex.Replace(functionName, @"[^a-zA-Z0-9_$]", "");

        // Ensure the name starts with a letter, underscore, or dollar sign
        if (!Regex.IsMatch(cleanedName, @"^[a-zA-Z_$]"))
        {
            cleanedName = "_" + cleanedName;
        }

        return cleanedName;
    }
}
