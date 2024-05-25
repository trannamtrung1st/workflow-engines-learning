
using System.Text.Json;
using WELearning.ConsoleApp.Testing.Processes;

// var process = RectangleAreaProcess.Build();
var process = RectanglePerimeterProcess.Build();

var processDesign = JsonSerializer.Serialize(process, new JsonSerializerOptions
{
    WriteIndented = true
});

Console.WriteLine(processDesign);