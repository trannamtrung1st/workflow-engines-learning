using Esprima;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintCompilationError : CompilationError
{
    private readonly ParserException _parserException;
    public JintCompilationError(ParserException parserException, int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _parserException = parserException;
        var originalPosition = RecalculatePosition(parserException, userContentLineStart, userContentLineEnd, userContentIndexStart, userContentIndexEnd);
        LineNumber = originalPosition.Line;
        Column = originalPosition.Column;
        Index = originalPosition.Index;
        IsSystemError = originalPosition.IsSystemError;
    }

    public override string Description => _parserException.Description;
    public override string RawMessage => _parserException.Message;
    public override int Index { get; }
    public override int LineNumber { get; }
    public override int Column { get; }
    public override bool IsSystemError { get; }

    public static (int Line, int Column, int Index, bool IsSystemError) RecalculatePosition(
        ParserException parserException, int userContentLineStart, int userContentLineEnd,
        int userContentIndexStart, int userContentIndexEnd)
    {
        var parserLine = parserException.LineNumber;
        var parserColumn = parserException.Column;
        var parserIndex = parserException.Index;
        if (parserLine == 0 || parserColumn == 0 || parserIndex < 0 || parserLine > userContentLineEnd || parserIndex > userContentIndexEnd)
            return (parserLine, parserColumn, parserIndex, true);
        var originalLine = parserLine - userContentLineStart + 1;
        return (originalLine, parserColumn, parserIndex - userContentIndexStart, false);
    }
}