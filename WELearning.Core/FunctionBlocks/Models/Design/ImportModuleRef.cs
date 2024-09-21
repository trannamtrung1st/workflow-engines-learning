namespace WELearning.Core.FunctionBlocks.Models.Design;

public record ImportModuleRef(string Id, string ModuleName, IEnumerable<string> BlockIds);
