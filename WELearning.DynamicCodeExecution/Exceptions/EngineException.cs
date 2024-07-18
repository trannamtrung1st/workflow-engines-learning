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
        int exLine, int exColumn, (int Start, int End) exLocation, int userContentLineStart, int userContentLineEnd,
        int userContentIndexStart, int userContentIndexEnd)
    {
        if (exLine == 0 || exColumn < 0 || exLocation.Start < 0 || exLine > userContentLineEnd || exLocation.Start > userContentIndexEnd)
            return (-1, -1, -1, -1);
        var originalLine = exLine - userContentLineStart + 1;
        return (
            originalLine, exColumn,
            exLocation.Start - userContentIndexStart,
            exLocation.End == -1 ? -1 : exLocation.End - userContentIndexStart
        );
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