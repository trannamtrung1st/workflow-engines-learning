using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessRunner<TFramework>
{
    Task Run(RunProcessRequest request, ProcessExecutionContext processContext, ProcessExecutionControl<TFramework> processControl);
}