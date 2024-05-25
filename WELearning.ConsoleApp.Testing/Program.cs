
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Extensions;

var serviceCollection = new ServiceCollection();
serviceCollection
    .AddDefaultProcessRunner()
    .AddDefaultBlockRunner()
    .AddDefaultLogicRunner()
    .AddDefaultBlockFrameworkFactory()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultRuntimeEngines();
var serviceProvider = serviceCollection.BuildServiceProvider();

// await TestEngines(serviceProvider);

await RunRectangleArea(serviceProvider);
await RunRectanglePerimeter(serviceProvider);

static async Task RunRectangleArea(ServiceProvider serviceProvider)
{
    var process = RectangleAreaProcess.Build();
    var processRunner = serviceProvider.GetRequiredService<IProcessRunner>();
    var bindings = new HashSet<ProcessVariableBinding>();
    var arguments = (Length: 5, Width: 2);
    bindings.Add(new(blockId: "Multiply", variableName: "X", value: arguments.Length));
    bindings.Add(new(blockId: "Multiply", variableName: "Y", value: arguments.Width));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl();
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.BlockExecutionMap["Multiply"].Control.OutputSnapshot["Result"];
    Console.WriteLine(finalResult);
}

static async Task RunRectanglePerimeter(ServiceProvider serviceProvider)
{
    var process = RectanglePerimeterProcess.Build();
    var processRunner = serviceProvider.GetRequiredService<IProcessRunner>();
    var bindings = new HashSet<ProcessVariableBinding>();
    var arguments = (Length: 5, Width: 2);
    bindings.Add(new(blockId: "Add", variableName: "X", value: arguments.Length));
    bindings.Add(new(blockId: "Add", variableName: "Y", value: arguments.Width));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl();
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.BlockExecutionMap["Multiply"].Control.OutputSnapshot["Result"];
    Console.WriteLine(finalResult);
}

static async Task TestEngines(ServiceProvider serviceProvider)
{
    await Task.CompletedTask;
}
