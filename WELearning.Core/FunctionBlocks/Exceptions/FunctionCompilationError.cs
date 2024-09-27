using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks.Exceptions;

public class FunctionCompilationError : BlockException
{
    public FunctionCompilationError(CompilationError error, Function function)
    {
        Error = error;
        Function = function;
    }

    public CompilationError Error { get; }
    public Function Function { get; }
    public override string Message => Error.Message;

    public void PrintErrorLocation(string locator = "->", ILogger logger = null)
        => Error.PrintErrorLocation(Function.Content, locator, logger);
}
