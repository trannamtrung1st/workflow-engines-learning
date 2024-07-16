using System.Text.RegularExpressions;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class SyntaxHelper
{
    public static bool HasAsyncSyntax(string content) => content.Contains("await "); // [NOTE] just for convenience, should use other mechanism or manually set

    public static string CleanJsFunctionSignature(string functionName)
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

    public static char[] GetJsVariableInvalidCharacters(string variableName)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            return new char[0];
        }

        List<char> invalidCharacters = new List<char>();
        for (int i = 0; i < variableName.Length; i++)
        {
            char c = variableName[i];
            if (i == 0)
            {
                // The first character must be a letter, underscore, or dollar sign
                if (!char.IsLetter(c) && c != '_' && c != '$')
                {
                    invalidCharacters.Add(c);
                }
            }
            else
            {
                // Subsequent characters can include letters, digits, underscores, or dollar signs
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '$')
                {
                    invalidCharacters.Add(c);
                }
            }
        }

        return invalidCharacters.ToArray();
    }
}
