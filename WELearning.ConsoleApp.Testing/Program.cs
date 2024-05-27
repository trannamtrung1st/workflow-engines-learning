
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.Core.Helpers;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
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
using var rootServiceProvider = serviceCollection.BuildServiceProvider();
using var scope = rootServiceProvider.CreateScope();
var serviceProvider = scope.ServiceProvider;

// await TestEngines.Run(serviceProvider,
//     timeoutTokenProvider: () => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

await TestFunctionBlocks.Run(serviceProvider,
    timeoutTokenProvider: () => new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

static class TestEngines
{
    public static async Task Run(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        var runtimeEngineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        await TestV8Lib(runtimeEngineFactory, cancellationToken: timeoutTokenProvider());
    }

    public static async Task TestV8Lib(IRuntimeEngineFactory engineFactory, CancellationToken cancellationToken)
    {
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        await runtimeEngine.Execute(JavascriptHelper.WrapTopLevelAsyncCall(@$"
            const sleep = (delay) => new Promise((resolve) => setTimeout(resolve, delay));
            await sleep(5000);
            
            const axiosResponse = await axios.get('https://cat-fact.herokuapp.com/facts');
            console.log(axiosResponse); 
            
            const fetchResponse = await fetch('https://cat-fact.herokuapp.com/facts');
            const fetchJson = await fetchResponse.json();
            console.log(fetchJson); 
        "),
        arguments: default(object),
        imports: new[]
        {
        "import 'axios.min.js'",
        "import { fetch } from 'fetch.min.js'"
        },
        types: null,
        assemblies: null,
        cancellationToken: cancellationToken);
    }
}

static class TestFunctionBlocks
{
    public static async Task Run(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        var processRunner = serviceProvider.GetService<IProcessRunner>();
        var blockRunner = serviceProvider.GetService<IBlockRunner<AppFramework>>();
        var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory<AppFramework>>();

        await RunBlockRandomDouble(blockRunner, blockFrameworkFactory, block: PredefinedBlocks.RandomCsScript, cancellationToken: timeoutTokenProvider());

        await RunBlockFactorial(blockRunner, blockFrameworkFactory, block: PredefinedBlocks.FactorialCsScript, cancellationToken: timeoutTokenProvider());

        var rectangleAreaProcess = RectangleAreaProcess.Build(
            bMultiply: new(PredefinedBlocks.MultiplyCsScript)
        );
        await RunRectangleArea(processRunner, process: rectangleAreaProcess, cancellationToken: timeoutTokenProvider());

        await RunRectanglePerimeter(processRunner, process: RectanglePerimeterProcess.Build(
            bAdd: new(PredefinedBlocks.AddCsScript), bMultiply: new(PredefinedBlocks.MultiplyCsCompiled)
        ), cancellationToken: timeoutTokenProvider());

        await RunRectanglePerimeter(processRunner, process: RectanglePerimeterProcess.Build(
            bAdd: new(PredefinedBlocks.AddJs), bMultiply: new(PredefinedBlocks.MultiplyCsCompiled)
        ), cancellationToken: timeoutTokenProvider());

        await RunLoopProcess(processRunner, process: LoopProcess.Build(), cancellationToken: timeoutTokenProvider());

        await RunDependencyWait(processRunner, process: DependencyWaitProcess.Build(), cancellationToken: timeoutTokenProvider());
    }

    public static async Task RunBlockRandomDouble(
        IBlockRunner<AppFramework> blockRunner,
        IBlockFrameworkFactory<AppFramework> blockFrameworkFactory,
        FunctionBlock block,
        CancellationToken cancellationToken)
    {
        var blockInstance = new FunctionBlockInstance(block);
        var control = new BlockExecutionControl(blockInstance);
        var blockFramework = blockFrameworkFactory.Create(control);
        var runRequest = new RunBlockRequest(blockInstance, triggerEvent: null);
        var result = await blockRunner.Run(runRequest, control, blockFramework, cancellationToken);
        Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
        Console.WriteLine(control.GetOutput("Result"));
    }

    public static async Task RunBlockFactorial(
        IBlockRunner<AppFramework> blockRunner,
        IBlockFrameworkFactory<AppFramework> blockFrameworkFactory,
        FunctionBlock block,
        CancellationToken cancellationToken)
    {
        var blockInstance = new FunctionBlockInstance(block);
        var control = new BlockExecutionControl(blockInstance);
        control.GetInput("N").Value = 5;
        var blockFramework = blockFrameworkFactory.Create(control);
        var runRequest = new RunBlockRequest(blockInstance, triggerEvent: null);
        var result = await blockRunner.Run(runRequest, control, blockFramework, cancellationToken);
        Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
        Console.WriteLine(control.GetOutput("Result"));
    }

    public static async Task RunRectangleArea(IProcessRunner processRunner, FunctionBlockProcess process,
        CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        var arguments = (Length: 5, Width: 2);
        bindings.Add(new(blockId: "Multiply", binding: new(variableName: "X", value: arguments.Length)));
        bindings.Add(new(blockId: "Multiply", binding: new(variableName: "Y", value: arguments.Width)));
        var runRequest = new RunProcessRequest(process);
        var processContext = new ProcessExecutionContext(bindings);
        var processControl = new ProcessExecutionControl(process, processContext);
        await processRunner.Run(runRequest, processContext, processControl, cancellationToken);

        var finalResult = processControl.GetBlockControl("Multiply").GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunRectanglePerimeter(IProcessRunner processRunner, FunctionBlockProcess process,
        CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        var arguments = (Length: 5, Width: 2);
        bindings.Add(new(blockId: "Add", binding: new(variableName: "X", value: arguments.Length)));
        bindings.Add(new(blockId: "Add", binding: new(variableName: "Y", value: arguments.Width)));
        var runRequest = new RunProcessRequest(process);
        var processContext = new ProcessExecutionContext(bindings);
        var processControl = new ProcessExecutionControl(process, processContext);
        await processRunner.Run(runRequest, processContext, processControl, cancellationToken);

        var finalResult = processControl.GetBlockControl("Multiply").GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunLoopProcess(IProcessRunner processRunner, FunctionBlockProcess process,
        CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "LoopController", binding: new(variableName: "N", value: 1000)));
        var runRequest = new RunProcessRequest(process);
        var processContext = new ProcessExecutionContext(bindings);
        var processControl = new ProcessExecutionControl(process, processContext);
        await processRunner.Run(runRequest, processContext, processControl, cancellationToken);

        var finalResult = processControl.GetBlockControl("LoopController").GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunDependencyWait(IProcessRunner processRunner, FunctionBlockProcess process,
        CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Delay", binding: new(variableName: "Ms", value: 3000)));
        bindings.Add(new(blockId: "Add1", binding: new(variableName: "X", value: 1)));
        bindings.Add(new(blockId: "Add1", binding: new(variableName: "Y", value: 2)));
        bindings.Add(new(blockId: "Add2", binding: new(variableName: "X", value: 3)));
        bindings.Add(new(blockId: "Add2", binding: new(variableName: "Y", value: 4)));
        var runRequest = new RunProcessRequest(process);
        var processContext = new ProcessExecutionContext(bindings);
        var processControl = new ProcessExecutionControl(process, processContext);
        await processRunner.Run(runRequest, processContext, processControl, cancellationToken);

        var finalResult = processControl.GetBlockControl("Add3").GetOutput("Result");
        Console.WriteLine(finalResult);
    }
}

// [TODO] add performance test
// [TODO] add inout variable
// [TODO] enhance CFB