
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Runtime;

var serviceCollection = new ServiceCollection();
serviceCollection
    .AddDefaultProcessRunner()
    .AddDefaultBlockRunner();
var serviceProvider = serviceCollection.BuildServiceProvider();

await RunRectanglePerimeter(serviceProvider);

static async Task RunRectanglePerimeter(ServiceProvider serviceProvider)
{
    var process = RectanglePerimeterProcess.Build();

    {
        var processDesign = JsonSerializer.Serialize(process, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(processDesign);
    }

    {
        var processRunner = serviceProvider.GetRequiredService<IProcessRunner>();
        var bindings = new HashSet<ProcessVariableBinding>();
        var arguments = (Length: 3, Width: 2);
        bindings.Add(new(blockId: "Add", variableName: "X", value: arguments.Length));
        bindings.Add(new(blockId: "Add", variableName: "Y", value: arguments.Width));
        var runRequest = new RunProcessRequest(process);
        var processContext = new ProcessExecutionContext(bindings);
        var processControl = new ProcessExecutionControl();
        await processRunner.Run(runRequest, processContext, processControl);
    }
}