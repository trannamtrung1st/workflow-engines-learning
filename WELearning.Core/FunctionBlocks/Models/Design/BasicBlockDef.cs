using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BasicBlockDef : BaseBlockDef
{
    public BasicBlockDef(string id, string name) : base(id, name)
    {
    }

    public BlockECC ExecutionControlChart { get; set; }
    public IEnumerable<Function> Functions { get; set; }
    public IEnumerable<string> ImportBlockIds { get; set; }

    private IEnumerable<ModuleFunction> _moduleFunctions;
    public IEnumerable<ModuleFunction> GetModuleFunctions()
    {
        if (_moduleFunctions != null) return _moduleFunctions;
        var functions = new List<ModuleFunction>();
        var exportedFunctions = Functions.Where(f => f.Exported);
        var inputs = new List<string>();
        var outputs = new List<string>();

        foreach (var variable in Variables)
        {
            if (variable.CanInput()) inputs.Add(variable.Name);
            if (variable.CanOutput()) outputs.Add(variable.Name);
        }

        foreach (var function in exportedFunctions)
        {
            functions.Add(new(
                signature: function.Signature,
                async: function.Async,
                content: function.Content,
                inputs: inputs, outputs: outputs,
                useRawContent: function.UseRawContent
            ));
        }

        _moduleFunctions = functions;
        return _moduleFunctions;
    }
}