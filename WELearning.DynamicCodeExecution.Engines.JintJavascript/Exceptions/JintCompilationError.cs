using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintCompilationError : CompilationError
{
    private readonly Acornima.ParseErrorException _parserException;
    public JintCompilationError(Acornima.ParseErrorException parserException, int userContentLineStart, int userContentLineEnd, int userContentIndexStart, int userContentIndexEnd)
    {
        _parserException = parserException;
        UnderlyingException = parserException;
        (int Line, int Column, int Index) = RecalculatePosition(
            parserException.LineNumber, parserException.Column, parserException.Error.Index,
            userContentLineStart, userContentLineEnd, userContentIndexStart, userContentIndexEnd);
        LineNumber = Line;
        this.Column = Column;
        this.Index = Index;
    }

    public override string Description => _parserException.Description;
    public override string RawMessage => _parserException.Message;
    public override string Source { get => _parserException.Source; }
}