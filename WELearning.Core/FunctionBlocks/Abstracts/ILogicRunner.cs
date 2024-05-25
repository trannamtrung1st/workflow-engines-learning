using WELearning.Core.FunctionBlocks.Models.Design;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ILogicRunner
{
    Task<TReturn> Run<TReturn>(Logic logic, object arguments = null);
    Task Run(Logic logic, object arguments = null);
}