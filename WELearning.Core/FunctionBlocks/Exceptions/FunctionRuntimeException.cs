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

    public void PrintError(string locator = "->")
    {
        var exceptionIndex = Exception.Index == -1 ? 0 : Exception.Index;
        var left = Function.Content.Substring(0, exceptionIndex);
        var right = Function.Content.Substring(exceptionIndex);
        Console.Write(left);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(locator);
        Console.ResetColor();
        Console.Write(right);
        Console.WriteLine();
    }
}