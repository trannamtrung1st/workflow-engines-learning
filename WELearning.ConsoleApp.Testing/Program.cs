
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Extensions;

var serviceCollection = new ServiceCollection()
    .AddDefaultProcessRunner<AppFramework>()
    .AddDefaultBlockRunner<AppFramework>()
    .AddDefaultLogicRunner<AppFramework>()
    .AddBlockFrameworkFactory<AppFramework, AppFrameworkFactory>()
    .AddDefaultRuntimeEngineFactory()
    .AddCSharpCompiledEngine()
    .AddCSharpScriptEngine()
    .AddV8JavascriptEngine(options => options.LibraryFolderPath = "/Users/trungtran/MyPlace/Personal/Learning/workflow-engines-learning/local/libs");
var serviceProvider = serviceCollection.BuildServiceProvider();
var processRunner = serviceProvider.GetService<IProcessRunner>();
var blockRunner = serviceProvider.GetService<IBlockRunner<AppFramework>>();
var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory<AppFramework>>();

await RunBlockRandomDouble(blockRunner, blockFrameworkFactory, block: PredefinedBlocks.RandomCsScript);

await RunBlockFactorial(blockRunner, blockFrameworkFactory, block: PredefinedBlocks.FactorialCsScript);

var rectangleAreaProcess = RectangleAreaProcess.Build(
    bMultiply: PredefinedBlocks.MultiplyCsScript
);
await RunRectangleArea(processRunner, process: rectangleAreaProcess);

await RunRectanglePerimeter(processRunner, process: RectanglePerimeterProcess.Build(
    bAdd: PredefinedBlocks.AddCsScript, bMultiply: PredefinedBlocks.MultiplyCsCompiled
));

await RunRectanglePerimeter(processRunner, process: RectanglePerimeterProcess.Build(
    bAdd: PredefinedBlocks.AddJs, bMultiply: PredefinedBlocks.MultiplyCsCompiled
));

await RunLoopProcess(processRunner, process: LoopProcess.Build());

static async Task RunBlockRandomDouble(
    IBlockRunner<AppFramework> blockRunner,
    IBlockFrameworkFactory<AppFramework> blockFrameworkFactory,
    FunctionBlock block)
{
    var control = new BlockExecutionControl(block);
    var blockFramework = blockFrameworkFactory.Create(control);
    var runRequest = new RunBlockRequest(block, triggerEvent: null);
    var result = await blockRunner.Run(runRequest, control, blockFramework);
    Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
    Console.WriteLine(control.GetOutput("Result"));
}

static async Task RunBlockFactorial(
    IBlockRunner<AppFramework> blockRunner,
    IBlockFrameworkFactory<AppFramework> blockFrameworkFactory,
    FunctionBlock block)
{
    var control = new BlockExecutionControl(block);
    control.GetInput("N").Value = 5;
    var blockFramework = blockFrameworkFactory.Create(control);
    var runRequest = new RunBlockRequest(block, triggerEvent: null);
    var result = await blockRunner.Run(runRequest, control, blockFramework);
    Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
    Console.WriteLine(control.GetOutput("Result"));
}

static async Task RunRectangleArea(IProcessRunner processRunner, FunctionBlockProcess process)
{
    var bindings = new HashSet<ProcessVariableBinding>();
    var arguments = (Length: 5, Width: 2);
    bindings.Add(new(blockId: "Multiply", variableName: "X", value: arguments.Length));
    bindings.Add(new(blockId: "Multiply", variableName: "Y", value: arguments.Width));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl(process, processContext);
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.GetBlockControl("Multiply").GetOutput("Result");
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
    var processControl = new ProcessExecutionControl(process, processContext);
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.GetBlockControl("Multiply").GetOutput("Result");
    Console.WriteLine(finalResult);
}

static async Task RunLoopProcess(IProcessRunner processRunner, FunctionBlockProcess process)
{
    var bindings = new HashSet<ProcessVariableBinding>();
    bindings.Add(new(blockId: "LoopController", variableName: "N", value: 1000));
    var runRequest = new RunProcessRequest(process);
    var processContext = new ProcessExecutionContext(bindings);
    var processControl = new ProcessExecutionControl(process, processContext);
    await processRunner.Run(runRequest, processContext, processControl);

    var finalResult = processControl.GetBlockControl("LoopController").GetOutput("Result");
    Console.WriteLine(finalResult);
}

// [TODO] add dependency wait