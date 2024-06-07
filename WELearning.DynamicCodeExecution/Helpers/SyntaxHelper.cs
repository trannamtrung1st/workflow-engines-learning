namespace WELearning.DynamicCodeExecution.Helpers;

public static class SyntaxHelper
{
    public static bool HasAsyncSyntax(string content) => content.Contains("await "); // [NOTE] just for convenience, should use other mechanism or manually set
}
