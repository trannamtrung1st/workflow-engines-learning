using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks.Exceptions;

public class FunctionCompilationError : Exception
{
    public FunctionCompilationError(CompilationError error, Function function)
    {
        Error = error;
        Function = function;
    }

    public CompilationError Error { get; }
    public Function Function { get; }
    public override string Message => Error.Message;

    public void PrintError(string locator = "->", ILogger logger = null)
        => Error.PrintError(Function.Content, locator, logger);
}
