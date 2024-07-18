namespace WELearning.DynamicCodeExecution.Extensions;

public static class StringExtensions
{
    public static string[] BreakLines(this string content)
        => content?.Split('\n') ?? throw new ArgumentNullException(nameof(content));
}