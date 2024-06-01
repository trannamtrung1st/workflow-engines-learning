using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBasicEC : IExecutionControl
{
    BasicBlockDef Definition { get; }
    string CurrentState { get; }
    new BFBExecutionResult Result { get; }
}