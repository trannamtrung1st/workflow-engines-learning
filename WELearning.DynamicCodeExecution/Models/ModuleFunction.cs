namespace WELearning.DynamicCodeExecution.Models;

public class ModuleFunction
{
    public ModuleFunction(string signature, string content, IEnumerable<string> inputs, IEnumerable<string> outputs, bool async, bool useRawContent)
    {
        Signature = signature;
        Content = content;
        Inputs = inputs;
        Outputs = outputs;
        Async = async;
        UseRawContent = useRawContent;
    }

    public string Signature { get; }
    public string Content { get; }
    public bool Async { get; }
    public IEnumerable<string> Inputs { get; }
    public IEnumerable<string> Outputs { get; }
    public bool UseRawContent { get; }
}