using Microsoft.Extensions.Logging;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks.Exceptions;

public class FunctionRuntimeException : Exception
{
    public FunctionRuntimeException(RuntimeException exception, Function function)
    {
        Exception = exception;
        Function = function;
    }

    public RuntimeException Exception { get; }
    public Function Function { get; }

    public void PrintError(string locator = "->", ILogger logger = null)
        => Exception.PrintError(Function.Content, locator, logger);

    public bool IsGracefulTerminated() => Exception.UnderlyingException is BlockTerminatedException eTerminated && eTerminated.Graceful;
}