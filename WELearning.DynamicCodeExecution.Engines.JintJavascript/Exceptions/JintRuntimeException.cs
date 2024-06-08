using System.Text.RegularExpressions;
using Jint;
using Jint.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintRuntimeException : RuntimeException
{
    private string _source;

    public JintRuntimeException(JavaScriptException jsException, string mainFunction, string content, int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        var error = jsException.Error;
        var stack = error.Get("stack").AsString();
        var description = error.Get("message").AsString();
        SetUserExceptionDetails(
            underlyingException: jsException,
            source: SourceUser,
            stack: stack,
            description: description,
            content: content,
            mainFunction: mainFunction,
            userContentLineStart,
            userContentLineEnd,
            userContentIndexStart,
            userContentIndexEnd
        );
    }

    public JintRuntimeException(PromiseRejectedException rejectedException, string mainFunction, string content, int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        var rejectedValue = rejectedException.RejectedValue;
        var stack = rejectedValue.Get("stack").AsString();
        var description = rejectedValue.Get("message").AsString();
        SetUserExceptionDetails(
            underlyingException: rejectedException,
            source: SourceUser,
            stack: stack,
            description: description,
            content: content,
            mainFunction: mainFunction,
            userContentLineStart,
            userContentLineEnd,
            userContentIndexStart,
            userContentIndexEnd
        );
    }

    public JintRuntimeException(
        Exception systemException, bool isUserSource, Acornima.Position currentNodePosition, int currentNodeIndex,
        int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _source = isUserSource ? SourceUser : SourceSystem;
        var (currentLine, currentColumn) = currentNodePosition;
        UnderlyingException = systemException;
        var (Line, Column, Index) = RecalculatePosition(
            currentLine, exColumn: currentColumn + 1, // [NOTE] Jint wrong calculation
            currentNodeIndex, userContentLineStart, userContentLineEnd, userContentIndexStart, userContentIndexEnd);
        LineNumber = Line;
        this.Column = Column;
        this.Index = Index;
        Description = systemException.Message;
        RawMessage = systemException.Message;
    }

    public override string Source { get => _source; }

    protected virtual void SetUserExceptionDetails(
        Exception underlyingException, string source, string stack, string description, string content, string mainFunction,
        int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _source = source;
        UnderlyingException = underlyingException;
        var (Line, Column, Index) = GetStackRootExceptionPosition(content, mainFunction, stack);
        (Line, Column, Index) = RecalculatePosition(
            Line, Column, Index, userContentLineStart, userContentLineEnd, userContentIndexStart, userContentIndexEnd);
        LineNumber = Line;
        this.Column = Column;
        this.Index = Index;
        Description = description;
        RawMessage = underlyingException.Message;
    }
    private static (int Line, int Column, int Index) GetStackRootExceptionPosition(string content, string mainFunction, string stack)
    {
        Regex exPositionRegex = new(@$"at {mainFunction}.+?([\d]+:[\d]+)");
        var match = exPositionRegex.Match(stack);
        var exPositionStr = match.Groups[1].Value;
        var parts = exPositionStr.Split(':');
        var exLine = int.Parse(parts[0]);
        var exCol = int.Parse(parts[1]);
        var searchIndex = 0;
        var searchRow = 1;
        var searchCol = 1;
        while (searchRow < exLine && searchIndex < content.Length)
        {
            while (searchIndex < content.Length && content[searchIndex++] != '\n') ;
            searchRow++;
        }
        while (searchCol++ < exCol && searchIndex++ < content.Length) ;
        return (exLine, exCol, searchIndex);
    }
}