using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.DynamicCodeExecution.Exceptions;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class ExceptionExtensions
{
    public static bool IsGracefulTerminated(this RuntimeException ex) => ex.UnderlyingException is ManuallyTerminatedException eTerminated && eTerminated.Graceful;
}