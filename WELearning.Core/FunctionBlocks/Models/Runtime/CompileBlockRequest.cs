using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public record CompileBlockRequest(
    BasicBlockDef BlockDefinition, RunTokens Tokens, IEnumerable<ImportModule> ImportModules = null,
    IDictionary<string, IOptimizationScope> OptimizationScopes = null);
