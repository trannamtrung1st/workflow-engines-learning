using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class ProcessRunner<TFramework> : IProcessRunner
{
    private readonly IBlockRunner<TFramework> _blockRunner;
    private readonly IBlockFrameworkFactory<TFramework> _blockFrameworkFactory;
    public ProcessRunner(IBlockRunner<TFramework> blockRunner, IBlockFrameworkFactory<TFramework> blockFrameworkFactory)
    {
        _blockRunner = blockRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
    }

    public virtual async Task Run(RunProcessRequest request, ProcessExecutionContext processContext, IProcessExecutionControl processControl)
    {
        await processControl.Run(request, (runBlockRequest, blockControl) =>
        {
            var blockFramework = _blockFrameworkFactory.Create(blockControl);
            return _blockRunner.Run(runBlockRequest, blockControl, blockFramework);
        });
    }
}