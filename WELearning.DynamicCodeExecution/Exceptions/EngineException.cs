namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class EngineException : Exception
{
    public virtual Exception UnderlyingException { get; protected set; }
    public virtual string Description { get; protected set; }
    public virtual string RawMessage { get; protected set; }
    public virtual int LineNumber { get; protected set; }
    public virtual int Column { get; protected set; }
    public virtual int Index { get; protected set; }

    public static (int Line, int Column, int Index) RecalculatePosition(
        int exLine, int exColumn, int exIndex, int userContentLineStart, int userContentLineEnd,
        int userContentIndexStart, int userContentIndexEnd)
    {
        if (exLine == 0 || exColumn == 0 || exIndex < 0 || exLine > userContentLineEnd || exIndex > userContentIndexEnd)
            return (-1, -1, -1);
        var originalLine = exLine - userContentLineStart + 1;
        return (originalLine, exColumn, exIndex - userContentIndexStart);
    }

    public override string ToString() => UnderlyingException?.ToString() ?? ToString();
}