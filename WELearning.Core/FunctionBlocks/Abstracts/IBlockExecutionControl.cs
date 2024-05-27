using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface IBlockExecutionControl
{
    FunctionBlock Block { get; }
    string CurrentState { get; }
    Exception Exception { get; }
    EBlockExecutionStatus Status { get; }
    ValueObject GetInput(string key);
    ValueObject GetOutput(string key);
    ValueObject GetInternalData(string key);
    Task<BlockExecutionResult> Execute(string triggerEvent,
        Func<Logic, Task<bool>> EvaluateCondition,
        Func<Logic, Task> RunAction,
        Func<IEnumerable<string>> GetOutputEvents);
}