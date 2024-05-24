namespace WELearning.Core.FunctionBlocks.Models.Design;

public class Logic
{
    public Logic(string id, string name, string content, Runtime runtime)
    {
        Id = id;
        Name = name;
        Content = content;
        Runtime = runtime;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public Runtime Runtime { get; set; }
}