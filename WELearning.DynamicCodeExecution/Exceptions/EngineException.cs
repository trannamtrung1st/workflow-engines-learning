using Microsoft.Extensions.Logging;
using WELearning.DynamicCodeExecution.Helpers;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class EngineException : Exception
{
    public virtual Exception UnderlyingException { get; protected set; }
    public virtual string Description { get; protected set; }
    public virtual string RawMessage { get; protected set; }
    public virtual int LineNumber { get; protected set; }
    public virtual int LineNumberEnd { get; protected set; }
    public virtual int Column { get; protected set; }
    public virtual int ColumnEnd { get; protected set; }
    public virtual int StartIndex { get; protected set; }
    public virtual int EndIndex { get; protected set; }
    public override string Message => Description;

    public static (int Line, int LineEnd, int Column, int ColumnEnd, int StartIndex, int EndIndex) RecalculatePosition(
        int exLine, int exColumn, (int Start, int End) exLocation, UserContentInfo contentInfo)
        => DiagnosticHelper.RecalculatePosition(exLine, exColumn, exLocation, contentInfo);

    public override string ToString() => UnderlyingException?.ToString() ?? base.ToString();

    public void PrintError(string content, string locator = "->", ILogger logger = null)
    {
        var startIdx = StartIndex < 0 ? 0 : StartIndex;
        startIdx = startIdx >= content.Length ? content.Length : startIdx;
        var endIdx = EndIndex > startIdx ? EndIndex : startIdx;
        endIdx = endIdx >= content.Length ? content.Length : endIdx;
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
