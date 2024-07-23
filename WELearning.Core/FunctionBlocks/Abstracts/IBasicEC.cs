using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBasicEC : IExecutionControl
{
    Function RunningFunction { get; }
    BasicBlockDef Definition { get; }
    string CurrentState { get; }
    new BFBExecutionResult Result { get; }
    IFunctionFramework FunctionFramework { get; }
}