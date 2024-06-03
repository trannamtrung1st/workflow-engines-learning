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

    public void PrintError(string locator = "->")
    {
        var left = Function.Content.Substring(0, Error.Index);
        var right = Function.Content.Substring(Error.Index);
        Console.Write(left);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(locator);
        Console.ResetColor();
        Console.Write(right);
        Console.WriteLine();
    }
}