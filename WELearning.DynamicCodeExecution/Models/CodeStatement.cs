namespace WELearning.DynamicCodeExecution.Models;

public class CodeStatement
{
    public int LineNumber { get; set; }
    public int Column { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string FunctionName { get; set; }
    public string Source { get; set; }
}
