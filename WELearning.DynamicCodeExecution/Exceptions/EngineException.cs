using Microsoft.Extensions.Logging;

namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class EngineException : Exception
{
    public virtual Exception UnderlyingException { get; protected set; }
    public virtual string Description { get; protected set; }
    public virtual string RawMessage { get; protected set; }
    public virtual int LineNumber { get; protected set; }
    public virtual int Column { get; protected set; }
    public virtual int StartIndex { get; protected set; }
    public virtual int EndIndex { get; protected set; }

    public static (int Line, int Column, int StartIndex, int EndIndex) RecalculatePosition(
        int exLine, int exColumn, (int Start, int End) exLocation, string[] lines,
        int userContentLineStart, int userContentLineEnd,
        int userContentIndexStart, int userContentIndexEnd)
    {
        if (exLine == 0 || exColumn < 0 || exLocation.Start < 0)
            return (-1, -1, -1, -1);

        int originalLine; int originalColumn;
        int startIndex; int endIndex;
        if (exLine > userContentLineEnd || exLocation.End > userContentIndexEnd)
        {
            startIndex = userContentIndexEnd - userContentIndexStart + 1;
            endIndex = startIndex;
            originalLine = userContentLineEnd - userContentLineStart + 1;
            originalColumn = lines[userContentLineEnd - 1].Length;
        }
        else if (exLine < userContentLineStart || exLocation.Start < userContentIndexStart)
        {
            startIndex = 0;
            endIndex = startIndex;
            originalLine = 1;
            originalColumn = 0;
        }
        else
        {
            startIndex = exLocation.Start - userContentIndexStart;
            endIndex = exLocation.End == -1 ? -1 : exLocation.End - userContentIndexStart;
            originalLine = exLine - userContentLineStart + 1;
            originalColumn = exColumn;
        }

        return (originalLine, originalColumn, startIndex, endIndex);
    }

    public override string ToString() => UnderlyingException?.ToString() ?? base.ToString();

    public void PrintError(string content, string locator = "->", ILogger logger = null)
    {
        var startIdx = StartIndex < 0 ? 0 : StartIndex;
        var endIdx = EndIndex > StartIndex ? EndIndex : StartIndex;
        var left = content[..startIdx];
        var error = content[startIdx..endIdx];
        var right = content[endIdx..];
        if (logger == null)
        {
            Console.Write(left);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(locator + error);
            Console.ResetColor();
            Console.Write(right);
            Console.WriteLine("\n");
        }
        else
        {
            var message = $"{left}{locator}{error}{right}\n";
            logger.LogError(message);
        }
    }
}