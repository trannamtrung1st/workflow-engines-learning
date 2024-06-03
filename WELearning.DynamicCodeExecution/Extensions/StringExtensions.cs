namespace WELearning.DynamicCodeExecution.Extensions;

public static class StringExtensions
{
    public static int NewLineCount(this string content)
        => content?.Split('\n').Length ?? throw new ArgumentNullException(nameof(content));
}