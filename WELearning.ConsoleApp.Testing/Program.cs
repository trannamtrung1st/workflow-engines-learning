
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
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
var processRunner = serviceProvider.GetService<IProcessRunner>();

var rectangleAreaProcess = RectangleAreaProcess.Build(
    bMultiply: PredefinedBlocks.MultiplyCsScript
);
await RunRectangleArea(processRunner, process: rectangleAreaProcess);

var rectanglePerimeterProcess = RectanglePerimeterProcess.Build(
    bAdd: PredefinedBlocks.Add, bMultiply: PredefinedBlocks.MultiplyCsCompiled
);
await RunRectanglePerimeter(processRunner, process: rectanglePerimeterProcess);
await RunRectanglePerimeter(processRunner, process: rectanglePerimeterProcess);

static async Task RunRectangleArea(IProcessRunner processRunner, FunctionBlockProcess process)
{
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

static async Task RunRectanglePerimeter(IProcessRunner processRunner, FunctionBlockProcess process)
{
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
