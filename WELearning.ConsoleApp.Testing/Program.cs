
using System.Text.Json;
using WELearning.ConsoleApp.Testing.Processes;

var process = RectangleAreaProcess.Build();

// [TODO] perimeter

var processDesign = JsonSerializer.Serialize(process, new JsonSerializerOptions
{
    WriteIndented = true
});

Console.WriteLine(processDesign);