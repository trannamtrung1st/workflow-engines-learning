using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public record CompileBlockRequest(BasicBlockDef BlockDefinition, RunTokens Tokens, ISet<IDisposable> OptimizationScopes = null);
