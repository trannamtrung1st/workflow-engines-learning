using System.Text.RegularExpressions;
using Jint;
using Jint.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintRuntimeException : RuntimeException
{
    private string _source;

    public JintRuntimeException(
        JavaScriptException jsException, string mainFunction,
        (int Start, int End) currentNodeLocation, UserContentInfo contentInfo)
    {
        var stack = jsException.JavaScriptStackTrace;
        var description = jsException.Message;
        SetUserExceptionDetails(
            underlyingException: jsException,
            source: SourceUser,
            stack: stack,
            description: description,
            mainFunction: mainFunction,
            currentNodeLocation, contentInfo
        );
    }

    public JintRuntimeException(
        PromiseRejectedException rejectedException, string mainFunction,
        (int Start, int End) currentNodeLocation, UserContentInfo contentInfo)
    {
        var rejectedValue = rejectedException.RejectedValue;
        var stack = rejectedValue.Get("stack").AsString();
        var description = rejectedValue.Get("message").AsString();
        SetUserExceptionDetails(
            underlyingException: rejectedException,
            source: SourceUser,
            stack: stack,
            description: description,
            mainFunction: mainFunction,
            currentNodeLocation, contentInfo
        );
    }

    public JintRuntimeException(
        Exception systemException, bool isUserSource, Acornima.Position currentNodePosition,
        (int Start, int End) currentNodeLocation, UserContentInfo contentInfo)
    {
        _source = isUserSource ? SourceUser : SourceSystem;
        var (currentLine, currentColumn) = currentNodePosition;
        UnderlyingException = systemException;
        (LineNumber, LineNumberEnd, Column, ColumnEnd, StartIndex, EndIndex) = RecalculatePosition(
            currentLine, exColumn: currentColumn + 1, // [NOTE] Jint wrong calculation
            currentNodeLocation, contentInfo);
        Description = systemException.Message;
        RawMessage = systemException.Message;
    }

    public override string Source { get => _source; }

    protected virtual void SetUserExceptionDetails(
        Exception underlyingException, string source, string stack, string description, string mainFunction,
        (int Start, int End) currentNodeLocation, UserContentInfo contentInfo)
    {
        _source = source;
        UnderlyingException = underlyingException;
        (LineNumber, Column) = GetStackRootExceptionPosition(mainFunction, stack);
        (LineNumber, LineNumberEnd, Column, ColumnEnd, this.StartIndex, this.EndIndex) = RecalculatePosition(
            LineNumber, Column, exLocation: currentNodeLocation, contentInfo);
        Description = description;
        RawMessage = underlyingException.Message;
    }
    private static (int Line, int Column) GetStackRootExceptionPosition(string mainFunction, string stack)
    {
        if (stack == null) return (-1, -1);
        Regex exPositionRegex = new(@$"at {mainFunction}.+?([\d]+:[\d]+)", RegexOptions.RightToLeft);
        var match = exPositionRegex.Match(stack);
        var exPositionStr = match.Groups[1].Value;
        var parts = exPositionStr.Split(':', options: StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return (-1, -1);
        var exLine = int.Parse(parts[0]);
        var exCol = int.Parse(parts[1]);
        return (exLine, exCol);
    }
}
