using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessRunner
{
    Task Run(RunProcessRequest request, ProcessExecutionContext processContext, ProcessExecutionControl processControl);
}