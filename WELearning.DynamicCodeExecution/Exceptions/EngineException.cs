namespace WELearning.DynamicCodeExecution.Exceptions;

public abstract class EngineException : Exception
{
    public virtual string Description { get; protected set; }
    public virtual string RawMessage { get; protected set; }
    public virtual int LineNumber { get; protected set; }
    public virtual int Column { get; protected set; }
    public virtual int Index { get; protected set; }
    public virtual bool IsUserContent { get; protected set; }

    public static (int Line, int Column, int Index, bool IsUserContent) RecalculatePosition(
        int exLine, int exColumn, int exIndex, int userContentLineStart, int userContentLineEnd,
        int userContentIndexStart, int userContentIndexEnd)
    {
        if (exLine == 0 || exColumn == 0 || exIndex < 0 || exLine > userContentLineEnd || exIndex > userContentIndexEnd)
            return (exLine, exColumn, exIndex, true);
        var originalLine = exLine - userContentLineStart + 1;
        return (originalLine, exColumn, exIndex - userContentIndexStart, false);
    }
}