
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;

namespace WELearning.Core.FunctionBlocks;

public class LogicRunner : ILogicRunner
{
    private readonly IRuntimeEngineFactory _engineFactory;
    private static readonly Assembly[] DefaultAssemblies = new[] { typeof(LogicRunner).Assembly };

    public LogicRunner(IRuntimeEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    public async Task<TReturn> Run<TReturn>(Logic logic, BlockGlobalObject globalObject = null, CancellationToken cancellationToken = default)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        var result = await engine.Execute<TReturn, BlockGlobalObject>(
            content: logic.Content,
            arguments: globalObject,
            imports: logic.Imports,
            assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies,
            cancellationToken: cancellationToken
        );
        return result;
    }

    public async Task Run(Logic logic, BlockGlobalObject globalObject = null, CancellationToken cancellationToken = default)
    {
        var engine = _engineFactory.CreateEngine(runtime: logic.Runtime);
        await engine.Execute(
            content: logic.Content,
            arguments: globalObject,
            imports: logic.Imports,
            assemblies: logic.Assemblies != null ? logic.Assemblies.Concat(DefaultAssemblies) : DefaultAssemblies,
            cancellationToken: cancellationToken
        );
    }
}