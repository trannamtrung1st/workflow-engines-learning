
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using WELearning.ConsoleApp.Testing.Processes;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.Core.Helpers;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;

const string LibraryFolderPath = "/Users/trungtran/MyPlace/Personal/Learning/workflow-engines-learning/local/libs";
var serviceCollection = new ServiceCollection()
    .AddDefaultProcessRunner<AppFramework>()
    .AddDefaultBlockRunner<AppFramework>()
    .AddDefaultLogicRunner<AppFramework>()
    .AddBlockFrameworkFactory<AppFramework, AppFrameworkFactory>()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddCSharpCompiledEngine()
    .AddCSharpScriptEngine()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options => options.LibraryFolderPath = LibraryFolderPath)
    .AddV8JavascriptEngine(options => options.LibraryFolderPath = LibraryFolderPath);
using var rootServiceProvider = serviceCollection.BuildServiceProvider();
using var scope = rootServiceProvider.CreateScope();
var serviceProvider = scope.ServiceProvider;
var timeoutTokenProvider = () => new CancellationTokenSource(TimeSpan.FromSeconds(100)).Token;

await TestEngines.BenchmarkLoops(serviceProvider, timeoutTokenProvider);
Console.WriteLine();
await TestFunctionBlocks.BenchmarkComplexProcess(serviceProvider, timeoutTokenProvider);
Console.WriteLine();
await TestEngines.Run(serviceProvider, timeoutTokenProvider);
Console.WriteLine();
await TestFunctionBlocks.Run(serviceProvider, timeoutTokenProvider);

// === Definitions ===

static class TestEngines
{
    public static async Task BenchmarkLoops(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        Console.WriteLine("=== Benchmark loops ===");
        var runtimeEngineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        var csCompiledEngine = runtimeEngineFactory.CreateEngine(ERuntime.CSharpCompiled);
        var csScriptEngine = runtimeEngineFactory.CreateEngine(ERuntime.CSharpScript);
        var jsEngine = runtimeEngineFactory.CreateEngine(ERuntime.Javascript) as IOptimizableRuntimeEngine;
        var jsEngineName = jsEngine.GetType().Name;
        var compiledAssemblies = new[]
        {
            typeof(object).Assembly,
            Assembly.Load("System.Runtime"),
            typeof(LoopTestArgs).Assembly,
            typeof(IExecutable<>).Assembly
        };
        var imports = new[]
        {
            "System.Threading",
            "System.Threading.Tasks",
            typeof(IExecutable<>).Namespace
        };
        const int FirstLoop = 1;
        // const int SecondLoop = 100_000;
        const int SecondLoop = 1_000_000;

        var sw = Stopwatch.StartNew();
        await LoopCSharpCompiled(FirstLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("C# compiled (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpCompiled(SecondLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("C# compiled ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await LoopCSharpScript(FirstLoop, csScriptEngine, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("C# script (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpScript(SecondLoop, csScriptEngine, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("C# script ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await LoopJavascript(FirstLoop, jsEngine, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("{0} (1st): {1}", jsEngineName, sw.ElapsedMilliseconds);
        await LoopJavascript(SecondLoop, jsEngine, cancellationToken: timeoutTokenProvider());
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, SecondLoop, sw.ElapsedMilliseconds);
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        var runtimeEngineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        // await TestV8Lib(runtimeEngineFactory, cancellationToken: timeoutTokenProvider());
        await TestJintLib(runtimeEngineFactory, cancellationToken: timeoutTokenProvider());
    }

    public static async Task LoopCSharpCompiled(int n, IRuntimeEngine runtimeEngine, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        for (var i = 0; i < n; i++)
        {
            await runtimeEngine.Execute<int, LoopTestArgs>(
                content: @$"
                public class Function : IExecutable<int, LoopTestArgs> 
                {{
                    public Task<int> Execute(LoopTestArgs arguments, CancellationToken cancellationToken) 
                    {{
                        return Task.FromResult(arguments.X * 5);    
                    }}
                }}", arguments: new LoopTestArgs { X = i },
                imports: imports, assemblies: assemblies, types: null, cancellationToken);
        }
    }

    public static async Task LoopCSharpScript(int n, IRuntimeEngine runtimeEngine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < n; i++)
        {
            await runtimeEngine.Execute(
                content: @$"X * 5", arguments: new LoopTestArgs { X = i },
                imports: null, assemblies: null, types: null, cancellationToken);
        }
    }

    public static async Task LoopJavascript(int n, IOptimizableRuntimeEngine runtimeEngine, CancellationToken cancellationToken)
    {
        IDisposable scope = null;
        Guid optimizationScopeId = Guid.NewGuid();
        try
        {
            for (var i = 0; i < n; i++)
            {
                scope = await runtimeEngine.Execute(
                    content: JavascriptHelper.WrapModuleFunction(@$"return _FB_.X * 5"), arguments: new LoopTestArgs { X = i },
                    imports: null, assemblies: null, types: null,
                    optimizationScopeId, cancellationToken);
            }
        }
        finally { scope?.Dispose(); }
    }

    private static readonly Func<Task<string>> TestAsync = async () =>
    {
        using var httpClient = new HttpClient();
        var result = await httpClient.GetStringAsync("https://github.com/sebastienros/esprima-dotnet");
        await Task.Delay(3000);
        return result;
    };

    public static async Task TestJintLib(IRuntimeEngineFactory engineFactory, CancellationToken cancellationToken)
    {
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);

        var result = await runtimeEngine.Execute<string, object>(JavascriptHelper.WrapModuleFunction(@"
            const testLodash = _.filter([1, 2, 3], item => !!item);
            const apiString = await _FB_.TestAsync();
            return `Hello ` + testLodash.toString() + apiString;
        ", topStatements: @"
            import './lodash.min.js';
            import './axios.min.js';
        "),
        arguments: new { TestAsync },
        imports: new[] { "import './fetch.min.js'" },
        types: null,
        assemblies: null,
        cancellationToken: cancellationToken);
    }

    public static async Task TestV8Lib(IRuntimeEngineFactory engineFactory, CancellationToken cancellationToken)
    {
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        var result = await runtimeEngine.Execute<string, object>(JavascriptHelper.WrapModuleFunction(@"
            const testLodash = _.filter([1, 2, 3], item => !!item);
            const apiString = await _FB_.TestAsync();
            return `Hello ` + testLodash.toString() + apiString;
        ", topStatements: @"
            import 'lodash.min.js';
            import 'axios.min.js';
        "),
        arguments: new { TestAsync },
        imports: new[] { "import { fetch } from 'fetch.min.js'" },
        types: null,
        assemblies: null,
        cancellationToken: cancellationToken);
    }
}

static class TestFunctionBlocks
{
    public static async Task BenchmarkComplexProcess(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        Console.WriteLine("=== Benchmark complex process ===");
        const int FirstLoop = 1;
        const int SecondLoop = 300;
        const int ThirdLoop = 700;
        var processRunner = serviceProvider.GetService<IProcessRunner>();
        var blockRunner = serviceProvider.GetService<IBlockRunner<AppFramework>>();
        var engineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        var logicRunner = serviceProvider.GetService<ILogicRunner<AppFramework>>();
        var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory<AppFramework>>();
        var jsEngine = engineFactory.CreateEngine(ERuntime.Javascript);
        var jsEngineName = jsEngine.GetType().Name;
        var csCompiledProcess = ComplexProcess.Build(
            bAddDef: PredefinedBlocks.AddCsCompiled,
            bMultiplyDef: PredefinedBlocks.MultiplyCsCompiled,
            bRandomDef: PredefinedBlocks.RandomCsCompiled,
            bDelayDef: PredefinedBlocks.DelayCsCompiled);
        IProcessExecutionControl CreateControl(FunctionBlockProcess process) => new ProcessExecutionControl<AppFramework>(process, blockRunner, logicRunner, blockFrameworkFactory);

        Task<double> RunCsCompiled() => RunComplexProcess(
            processRunner,
            CreateControl: () => CreateControl(process: csCompiledProcess),
            cancellationToken: timeoutTokenProvider());

        var csScriptProcess = ComplexProcess.Build(
            bAddDef: PredefinedBlocks.AddCsScript,
            bMultiplyDef: PredefinedBlocks.MultiplyCsScript,
            bRandomDef: PredefinedBlocks.RandomCsScript,
            bDelayDef: PredefinedBlocks.DelayCsScript);
        Task<double> RunCsScript() => RunComplexProcess(
            processRunner,
            CreateControl: () => CreateControl(process: csScriptProcess),
            cancellationToken: timeoutTokenProvider());

        var jsProcess = ComplexProcess.Build(
            bAddDef: PredefinedBlocks.AddJs,
            bMultiplyDef: PredefinedBlocks.MultiplyJs,
            bRandomDef: PredefinedBlocks.RandomJs,
            bDelayDef: PredefinedBlocks.DelayJs);
        Task<double> RunJs() => RunComplexProcess(
            processRunner,
            CreateControl: () => CreateControl(process: jsProcess),
            cancellationToken: timeoutTokenProvider());

        var sw = Stopwatch.StartNew();
        await Loop(FirstLoop, func: RunCsCompiled);
        Console.WriteLine("C# compiled (1st): {0}", sw.ElapsedMilliseconds);
        await Loop(SecondLoop, func: RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, func: RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", ThirdLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await Loop(FirstLoop, func: RunCsScript);
        Console.WriteLine("C# script (1st): {0}", sw.ElapsedMilliseconds);
        await Loop(SecondLoop, func: RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, func: RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", ThirdLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await Loop(FirstLoop, func: RunJs);
        Console.WriteLine("{0} (1st): {1}", jsEngineName, sw.ElapsedMilliseconds);
        await Loop(SecondLoop, func: RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, func: RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, ThirdLoop, sw.ElapsedMilliseconds);

        const int ParallelLoopCount = 1000;
        Console.WriteLine();
        Console.WriteLine("=== Benchmark complex process (Parallel) ===");
        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", ParallelLoopCount, sw.ElapsedMilliseconds);
        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", ParallelLoopCount, sw.ElapsedMilliseconds);
        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, ParallelLoopCount, sw.ElapsedMilliseconds);
    }

    public static async Task Loop(int n, Func<Task> func)
    {
        for (int i = 0; i < n; i++)
            await func();
    }

    public static async Task ParallelLoop(int n, Func<Task> func)
    {
        var waits = new List<TaskCompletionSource>();
        for (int i = 0; i < n; i++)
        {
            var tcs = new TaskCompletionSource();
            waits.Add(tcs);
            _ = Task.Factory.StartNew(function: async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult();
                }
                catch (Exception ex) { tcs.SetException(ex); }
            }, creationOptions: TaskCreationOptions.LongRunning);
        }
        await Task.WhenAll(waits.Select(w => w.Task));
    }

    public static async Task<double> RunComplexProcess(IProcessRunner processRunner, Func<IProcessExecutionControl> CreateControl, CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add1X", value: 5, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add1Y", value: 10, type: EBindingType.Output)));
        var processControl = CreateControl();
        var runRequest = new RunProcessRequest(process: processControl.Process, bindings);
        await processRunner.Run(runRequest, processControl, cancellationToken);

        var tcs = new TaskCompletionSource();
        processControl.Completed += (o, e) => tcs.SetResult();
        processControl.Failed += (o, e) => tcs.SetException(e);
        await tcs.Task;

        if (!processControl.TryGetBlockControl("Outputs", out var blockControl)) throw new Exception("Control not found");
        var finalResult = blockControl.GetInput("Result");
        return (double)finalResult.Value;
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<CancellationToken> timeoutTokenProvider)
    {
        var processRunner = serviceProvider.GetService<IProcessRunner>();
        var blockRunner = serviceProvider.GetService<IBlockRunner<AppFramework>>();
        var engineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        var logicRunner = serviceProvider.GetService<ILogicRunner<AppFramework>>();
        var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory<AppFramework>>();
        const int DelayMs = 5000;
        IProcessExecutionControl CreateProcessControl(FunctionBlockProcess process) => new ProcessExecutionControl<AppFramework>(process, blockRunner, logicRunner, blockFrameworkFactory);
        IBlockExecutionControl CreateBlockControl(FunctionBlockInstance block) => new BlockExecutionControl<AppFramework>(block: block, logicRunner, blockFrameworkFactory);

        Console.WriteLine("DelayJS {0} ms", DelayMs);
        await RunBlockDelay(
            blockRunner, CreateControl: () => CreateBlockControl(block: new(PredefinedBlocks.DelayJs)),
            delayMs: DelayMs, cancellationToken: timeoutTokenProvider());

        await RunBlockRandomDouble(blockRunner, CreateControl: () => CreateBlockControl(block: new(PredefinedBlocks.RandomCsScript)), cancellationToken: timeoutTokenProvider());

        await RunBlockFactorial(blockRunner, CreateControl: () => CreateBlockControl(block: new(PredefinedBlocks.FactorialCsScript)), cancellationToken: timeoutTokenProvider());

        var rectangleAreaProcess = RectangleAreaProcess.Build(
            bMultiply: new(PredefinedBlocks.MultiplyCsScript)
        );
        await RunRectangleArea(processRunner, CreateControl: () => CreateProcessControl(process: rectangleAreaProcess), cancellationToken: timeoutTokenProvider());

        await RunRectanglePerimeter(processRunner,
            CreateControl: () => CreateProcessControl(process: RectanglePerimeterProcess.Build(
                bAdd: new(PredefinedBlocks.AddCsScript), bMultiply: new(PredefinedBlocks.MultiplyCsCompiled)
            )),
            cancellationToken: timeoutTokenProvider());

        var rectanglePerimeterJs = RectanglePerimeterProcess.Build(
            bAdd: new(PredefinedBlocks.AddJs), bMultiply: new(PredefinedBlocks.MultiplyCsCompiled)
        );
        await RunRectanglePerimeter(processRunner, CreateControl: () => CreateProcessControl(process: rectanglePerimeterJs), cancellationToken: timeoutTokenProvider());
        Console.WriteLine("\n{0}\n", JsonSerializer.Serialize(rectanglePerimeterJs, Program.DefaultJsonOpts));

        await RunLoopProcess(processRunner, CreateControl: () => CreateProcessControl(process: LoopProcess.Build()), cancellationToken: timeoutTokenProvider());

        await RunDependencyWait(processRunner, CreateControl: () => CreateProcessControl(process: DependencyWaitProcess.Build()), cancellationToken: timeoutTokenProvider());
    }

    public static async Task RunBlockDelay(
        IBlockRunner<AppFramework> blockRunner, Func<IBlockExecutionControl> CreateControl,
        int delayMs, CancellationToken cancellationToken)
    {
        var control = CreateControl();
        var bindings = new VariableBinding[] { new("Ms", delayMs, type: EBindingType.Input) };
        var runRequest = new RunBlockRequest(block: control.Block, bindings, triggerEvent: null);
        var result = await blockRunner.Run(runRequest, control, optimizationScopeId: default, cancellationToken);
        Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
    }

    public static async Task RunBlockRandomDouble(
        IBlockRunner<AppFramework> blockRunner, Func<IBlockExecutionControl> CreateControl,
        CancellationToken cancellationToken)
    {
        var control = CreateControl();
        var runRequest = new RunBlockRequest(block: control.Block, bindings: Array.Empty<VariableBinding>(), triggerEvent: null);
        var result = await blockRunner.Run(runRequest, control, optimizationScopeId: default, cancellationToken);
        Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
        Console.WriteLine(control.GetOutput("Result"));
    }

    public static async Task RunBlockFactorial(
        IBlockRunner<AppFramework> blockRunner, Func<IBlockExecutionControl> CreateControl,
        CancellationToken cancellationToken)
    {
        var control = CreateControl();
        var bindings = new VariableBinding[] { new("N", 5, type: EBindingType.Input) };
        var runRequest = new RunBlockRequest(block: control.Block, bindings, triggerEvent: null);
        var result = await blockRunner.Run(runRequest, control, optimizationScopeId: default, cancellationToken);
        Console.WriteLine(string.Join(Environment.NewLine, result.OutputEvents));
        Console.WriteLine(control.GetOutput("Result"));
    }

    public static async Task RunRectangleArea(IProcessRunner processRunner, Func<IProcessExecutionControl> CreateControl, CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Length", value: 5, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Width", value: 2, type: EBindingType.Output)));
        var processControl = CreateControl();
        var runRequest = new RunProcessRequest(process: processControl.Process, bindings: bindings);
        await processRunner.Run(runRequest, processControl, cancellationToken);

        var tcs = new TaskCompletionSource();
        processControl.Completed += (o, e) => tcs.SetResult();
        processControl.Failed += (o, e) => tcs.SetException(e);
        await tcs.Task;

        if (!processControl.TryGetBlockControl("Outputs", out var blockControl)) throw new Exception("Control not found");
        var finalResult = blockControl.GetInput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunRectanglePerimeter(IProcessRunner processRunner, Func<IProcessExecutionControl> CreateControl, CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Length", value: 5, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Width", value: 2, type: EBindingType.Output)));
        var processControl = CreateControl();
        var runRequest = new RunProcessRequest(process: processControl.Process, bindings);
        await processRunner.Run(runRequest, processControl, cancellationToken);

        var tcs = new TaskCompletionSource();
        processControl.Completed += (o, e) => tcs.SetResult();
        processControl.Failed += (o, e) => tcs.SetException(e);
        await tcs.Task;

        if (!processControl.TryGetBlockControl("Outputs", out var blockControl)) throw new Exception("Control not found");
        var finalResult = blockControl.GetInput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunLoopProcess(IProcessRunner processRunner, Func<IProcessExecutionControl> CreateControl, CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "N", value: 1000, type: EBindingType.Output)));
        var processControl = CreateControl();
        var runRequest = new RunProcessRequest(process: processControl.Process, bindings);
        await processRunner.Run(runRequest, processControl, cancellationToken);

        var tcs = new TaskCompletionSource();
        processControl.Completed += (o, e) => tcs.SetResult();
        processControl.Failed += (o, e) => tcs.SetException(e);
        await tcs.Task;

        if (!processControl.TryGetBlockControl("Outputs", out var blockControl)) throw new Exception("Control not found");
        var finalResult = blockControl.GetInput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunDependencyWait(IProcessRunner processRunner, Func<IProcessExecutionControl> CreateControl, CancellationToken cancellationToken)
    {
        var bindings = new HashSet<ProcessVariableBinding>();
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "DelayMs", value: 3000, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add1X", value: 1, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add1Y", value: 2, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add2X", value: 3, type: EBindingType.Output)));
        bindings.Add(new(blockId: "Inputs", binding: new(variableName: "Add2Y", value: 4, type: EBindingType.Output)));
        var processControl = CreateControl();
        var runRequest = new RunProcessRequest(process: processControl.Process, bindings);
        await processRunner.Run(runRequest, processControl, cancellationToken);

        var tcs = new TaskCompletionSource();
        processControl.Completed += (o, e) => tcs.SetResult();
        processControl.Failed += (o, e) => tcs.SetException(e);
        await tcs.Task;

        if (!processControl.TryGetBlockControl("Outputs", out var blockControl)) throw new Exception("Control not found");
        var finalResult = blockControl.GetInput("Result");
        Console.WriteLine(finalResult);
    }
}

public class LoopTestArgs
{
    public int X { get; set; }
}

partial class Program
{
    public static readonly JsonSerializerOptions DefaultJsonOpts;

    static Program()
    {
        DefaultJsonOpts = new JsonSerializerOptions { WriteIndented = true };
        DefaultJsonOpts.Converters.Add(new JsonStringEnumConverter());
    }
}