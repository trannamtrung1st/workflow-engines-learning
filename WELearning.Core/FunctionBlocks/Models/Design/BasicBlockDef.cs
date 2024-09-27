using System.Collections.Concurrent;
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.Core.FunctionBlocks.Models.Design;

public class BasicBlockDef : BaseBlockDef
{
    public BasicBlockDef(string id, string name, ConcurrentDictionary<string, object> customData = null) : base(id, name, customData)
    {
    }

    public BlockECC ExecutionControlChart { get; set; }

    private bool _hasAsyncFunction;
    public bool HasAsyncFunction => _hasAsyncFunction;
    private bool _hasImport;
    public bool HasImport => _hasImport;

    private IEnumerable<Function> _functions;
    public IEnumerable<Function> Functions
    {
        get => _functions; set
        {
            _functions = value;
            if (_functions != null)
            {
                foreach (var func in _functions)
                {
                    if (func.Async)
                        _hasAsyncFunction = true;
                    if (func.HasImport)
                        _hasImport = true;
                }
            }
        }
    }

    public IEnumerable<ImportModuleRef> ImportModuleRefs { get; set; }
    private IEnumerable<ModuleFunction> _moduleFunctions;
    public IEnumerable<ModuleFunction> GetModuleFunctions()
    {
        if (_moduleFunctions != null)
            return _moduleFunctions;
        var functions = new List<ModuleFunction>();
        var exportedFunctions = Functions.Where(f => f.Exported);
        var inputs = new List<string>();
        var outputs = new List<string>();

        foreach (var variable in Variables)
        {
            if (variable.CanInput())
                inputs.Add(variable.Name);
            if (variable.CanOutput())
                outputs.Add(variable.Name);
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

    public (IEnumerable<string> Inputs, IEnumerable<string> Outputs) GetVariableNames()
    {
        var inputs = new List<string>();
        var outputs = new List<string>();

        foreach (var variable in Variables)
        {
            var source = variable.CanOutput() ? outputs : inputs;
            source.Add(variable.Name);
        }

        return (inputs, outputs);
    }
}
