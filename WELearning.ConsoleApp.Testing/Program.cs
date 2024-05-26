
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Extensions;

var serviceCollection = new ServiceCollection();
serviceCollection
    .AddDefaultProcessRunner<AppFrameworkInstance>()
    .AddDefaultBlockRunner<AppFrameworkInstance>()
    .AddDefaultLogicRunner<AppFrameworkInstance>()
    .AddBlockFrameworkFactory<AppFrameworkInstance, AppFrameworkFactory>()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultRuntimeEngines();
var serviceProvider = serviceCollection.BuildServiceProvider();
var processRunner = serviceProvider.GetService<IProcessRunner<AppFrameworkInstance>>();
var blockRunner = serviceProvider.GetService<IBlockRunner<AppFrameworkInstance>>();
var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory<AppFrameworkInstance>>();

await RunBlockRandomDouble(blockRunner, blockFrameworkFactory, block: PredefinedBlocks.RandomCsScript);

var rectangleAreaProcess = RectangleAreaProcess.Build(
    bMultiply: PredefinedBlocks.MultiplyCsScript
);
await RunRectangleArea(processRunner, process: rectangleAreaProcess);

var rectanglePerimeterProcess = RectanglePerimeterProcess.Build(
    bAdd: PredefinedBlocks.AddCsScript, bMultiply: PredefinedBlocks.MultiplyCsCompiled
);
await RunRectanglePerimeter(processRunner, process: rectanglePerimeterProcess);
await RunRectanglePerimeter(processRunner, process: rectanglePerimeterProcess);

static async Task RunBlockRandomDouble(
    IBlockRunner<AppFrameworkInstance> blockRunner,
    IBlockFrameworkFactory<AppFrameworkInstance> blockFrameworkFactory,
    FunctionBlock block)
{
    var control = new BlockExecutionControl(block.Id, block.ExecutionControlChart.InitialState);
    var blockFramework = blockFrameworkFactory.Create(control);
    var runRequest = new RunBlockRequest(block, triggerEvent: null);
    var result = await blockRunner.Run(runRequest, control, blockFramework);
    Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
    Console.WriteLine(control.OutputSnapshot["Result"]);
}

static async Task RunRectangleArea(IProcessRunner<AppFrameworkInstance> processRunner, FunctionBlockProcess process)
{
    var bindings = new HashSet<ProcessVariableBinding>();
    var arguments = (Length: 5, Width: 2);
    bindings.Add(new(blockId: "Multiply", variableName: "X", value: arguments.Length));
    bindings.Add(new(blockId: "Multiply", variableName: "Y", value: arguments.Width));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl<AppFrameworkInstance>();
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.BlockExecutionMap["Multiply"].Control.OutputSnapshot["Result"];
    Console.WriteLine(finalResult);
}

static async Task RunRectanglePerimeter(IProcessRunner<AppFrameworkInstance> processRunner, FunctionBlockProcess process)
{
    var bindings = new HashSet<ProcessVariableBinding>();
    var arguments = (Length: 5, Width: 2);
    bindings.Add(new(blockId: "Add", variableName: "X", value: arguments.Length));
    bindings.Add(new(blockId: "Add", variableName: "Y", value: arguments.Width));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl<AppFrameworkInstance>();
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.BlockExecutionMap["Multiply"].Control.OutputSnapshot["Result"];
    Console.WriteLine(finalResult);
}
