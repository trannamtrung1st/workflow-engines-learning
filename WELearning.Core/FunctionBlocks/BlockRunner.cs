using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Runtime;

namespace WELearning.Core.FunctionBlocks;

public class BlockRunner : IBlockRunner
{
    private readonly IFunctionRunner _functionRunner;

    public BlockRunner(IFunctionRunner functionRunner)
    {
        _functionRunner = functionRunner;
    }

    public async Task Compile(CompileBlockRequest request, Guid? optimizationScopeId)
    {
        var bDef = request.BlockDefinition;
        if (bDef.Functions?.Any() != true)
            return;
        var (inputs, outputs) = bDef.GetVariableNames();
        var optimizationScopes = request.OptimizationScopes ?? new HashSet<IDisposable>();

        try
        {
            foreach (var function in bDef.Functions)
            {
                var scope = await _functionRunner.Compile(function, inputs, outputs, modules: null, optimizationScopeId, tokens: request.Tokens);
                if (scope != null)
                    optimizationScopes.Add(scope);
            }
        }
        finally
        {
            if (request.OptimizationScopes is null)
                foreach (var optimizationScope in optimizationScopes)
                    optimizationScope.Dispose();
        }
    }

    public async Task Run(RunBlockRequest request, IExecutionControl control, Guid? optimizationScopeId)
    {
        bool started = false;
        while (!started)
        {
            control.WaitForIdle(request.Tokens.Combined);
            await control.MutexAccess(async () =>
            {
                if (control.IsIdle)
                {
                    await control.Execute(request, optimizationScopeId);
                    started = true;
                }
            }, request.Tokens.Combined);
        }
    }
}