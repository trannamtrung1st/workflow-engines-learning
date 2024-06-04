using System.Text.RegularExpressions;
using Esprima;
using Jint;
using Jint.Runtime;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintRuntimeException : RuntimeException
{
    private readonly string _source;
    public JintRuntimeException(PromiseRejectedException rejectedException, string content, int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _source = SourceUser;
        var rejectedValue = rejectedException.RejectedValue;
        var stack = rejectedValue.Get("stack").AsString();
        var (Line, Column, Index) = GetStackRootExceptionPosition(content, stack);
        (Line, Column, Index) = RecalculatePosition(
            Line, Column, Index, userContentLineStart, userContentLineEnd, userContentIndexStart, userContentIndexEnd);
        LineNumber = Line;
        this.Column = Column;
        this.Index = Index;
        Description = rejectedValue.Get("message").AsString();
        RawMessage = rejectedException.Message;
    }

    public JintRuntimeException(
        Exception systemException, Position currentNodePosition, int currentNodeIndex,
        int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _source = SourceSystem;
        var (currentLine, currentColumn) = currentNodePosition;
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

    private static readonly Regex _posRegex = new(@"([\d]+:[\d]+)");
    private static (int Line, int Column, int Index) GetStackRootExceptionPosition(string content, string stack)
    {
        var match = _posRegex.Match(stack);
        var exPosStr = match.Value;
        var parts = exPosStr.Split(':');
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