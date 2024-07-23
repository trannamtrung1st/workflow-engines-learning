using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Framework;

namespace WELearning.Samples.FBWorker.FunctionBlock;

public class DeviceFunctionFramework : FunctionFramework
{
    private readonly IExecutionControl _control;
    private readonly ILogger<DeviceFunctionFramework> _logger;

    public DeviceFunctionFramework(IExecutionControl control, ILogger<DeviceFunctionFramework> logger) : base(logger)
    {
        _logger = logger;
        _control = control;
    }
}