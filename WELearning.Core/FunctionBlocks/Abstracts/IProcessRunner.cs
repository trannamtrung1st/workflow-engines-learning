using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IProcessRunner<TFrameworkInstance>
{
    Task Run(RunProcessRequest request, ProcessExecutionContext processContext, ProcessExecutionControl<TFrameworkInstance> processControl);
}