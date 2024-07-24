using WELearning.DynamicCodeExecution.Exceptions;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;

public class JintCompilationError : CompilationError
{
    private readonly Acornima.ParseErrorException _parserException;
    public JintCompilationError(Acornima.ParseErrorException parserException, UserContentInfo contentInfo)
    {
        _parserException = parserException;
        UnderlyingException = parserException;
        (int Line, int Column, int StartIndex, int EndIndex) = RecalculatePosition(
            parserException.LineNumber, parserException.Column,
            exLocation: (parserException.Error.Index, -1), contentInfo);
        LineNumber = Line;
        this.Column = Column;
        this.StartIndex = StartIndex;
        this.EndIndex = EndIndex;
    }

    public override string Description => _parserException.Description;
    public override string RawMessage => _parserException.Message;
    public override string Source { get => _parserException.Source; }
}