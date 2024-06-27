
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WELearning.ConsoleApp.Testing.CompositeBlocks;
using WELearning.ConsoleApp.Testing.Entities;
using WELearning.ConsoleApp.Testing.Framework;
using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.Extensions;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Shared.Extensions;
using WELearning.Shared.Concurrency;
using WELearning.Shared.Concurrency.Abstracts;
using WELearning.Shared.Concurrency.Configurations;

const int minThreads = 512;
ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);

const string LibraryFolderPath = "/Users/trungtran/MyPlace/Personal/Learning/workflow-engines-learning/local/libs";
var serviceCollection = new ServiceCollection()
    .AddSingleton<DataStore>()
    .AddLogging(cfg => cfg.AddSimpleConsole())
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    .AddSingleton<ISyncAsyncTaskLimiter>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<SyncAsyncTaskLimiter>>();
        var options = new TaskLimiterOptions
        {
            InitialLimit = minThreads,
            AvailableCores = Environment.ProcessorCount,
            TargetCpuUtil = 0.75,
            WaitTime = 4000,
            ServiceTime = 25
        };
        return new SyncAsyncTaskLimiter(options, logger);
    })
    .AddSyncAsyncTaskRunner()
    .AddDefaultBlockRunner()
    .AddDefaultFunctionRunner()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddBlockFrameworkFactory<AppBlockFrameworkFactory>()
    .AddFunctionFramework<AppFunctionFramework>()
    .AddCSharpCompiledEngine()
    .AddCSharpScriptEngine()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options => options.LibraryFolderPath = LibraryFolderPath);

using var rootServiceProvider = serviceCollection.BuildServiceProvider();
using var terminationCts = new CancellationTokenSource();
var tokensProvider = () => new RunTokens(timeout: TimeSpan.FromSeconds(30), termination: terminationCts.Token);

async Task ExecuteWithScope(Func<IServiceProvider, Task> task)
{
    using var scope = rootServiceProvider.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    await task(serviceProvider);
}

await StartInputThread(terminationCts);

await ExecuteWithScope((serviceProvider) =>
    TestEngines.BenchmarkLoops(serviceProvider, tokensProvider));
Console.WriteLine();

await ExecuteWithScope((serviceProvider) =>
    TestFunctionBlocks.BenchmarkComplexCFB(serviceProvider, tokensProvider));
Console.WriteLine();

await ExecuteWithScope((serviceProvider) =>
    TestEngines.Run(serviceProvider, tokensProvider));
Console.WriteLine();

await ExecuteWithScope((serviceProvider) =>
    TestFunctionBlocks.Run(serviceProvider, tokensProvider));
Console.WriteLine();

// === Definitions ===

static class TestEngines
{
    public static async Task BenchmarkLoops(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        Console.WriteLine("=== Benchmark loops ===");
        var runtimeEngineFactory = serviceProvider.GetRequiredService<IRuntimeEngineFactory>();
        var csCompiledEngine = runtimeEngineFactory.CreateEngine(ERuntime.CSharpCompiled);
        var csScriptEngine = runtimeEngineFactory.CreateEngine(ERuntime.CSharpScript);
        var jsEngine = runtimeEngineFactory.CreateEngine(ERuntime.Javascript);
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
        const int SecondLoop = 100_000;
        // const int SecondLoop = 1_000_000; // [NOTE] high memory usage

        var sw = Stopwatch.StartNew();
        await LoopCSharpCompiled(FirstLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, tokensProvider);
        Console.WriteLine("C# compiled (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpCompiled(SecondLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, tokensProvider);
        Console.WriteLine("C# compiled ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await LoopCSharpScript(FirstLoop, csScriptEngine, tokensProvider);
        Console.WriteLine("C# script (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpScript(SecondLoop, csScriptEngine, tokensProvider);
        Console.WriteLine("C# script ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await LoopJavascript(FirstLoop, jsEngine, tokensProvider);
        Console.WriteLine("{0} (1st): {1}", jsEngineName, sw.ElapsedMilliseconds);
        await LoopJavascript(SecondLoop, jsEngine, tokensProvider);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, SecondLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        var runtimeEngineFactory = serviceProvider.GetRequiredService<IRuntimeEngineFactory>();
        // await TestV8Lib(runtimeEngineFactory, runTokens: tokensProvider());
        await TestJintLib(runtimeEngineFactory, runTokens: tokensProvider());
    }

    public static async Task LoopCSharpCompiled(int n, IRuntimeEngine runtimeEngine, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, Func<RunTokens> tokensProvider)
    {
        string contentId = Guid.NewGuid().ToString();
        for (var i = 0; i < n; i++)
        {
            using var runTokens = tokensProvider();
            await runtimeEngine.Execute<int, LoopTestArgs>(
                request: new(
                    content: @$"
                    public class Function : IExecutable<int, LoopTestArgs> 
                    {{
                        public Task<int> Execute(LoopTestArgs arguments, CancellationToken cancellationToken) 
                        {{
                            return Task.FromResult(arguments.X * 5);    
                        }}
                    }}",
                    contentId: contentId,
                    arguments: new LoopTestArgs { X = i },
                    imports: imports, assemblies: assemblies, types: null, tokens: runTokens
                ));
        }
    }

    public static async Task LoopCSharpScript(int n, IRuntimeEngine runtimeEngine, Func<RunTokens> tokensProvider)
    {
        string contentId = Guid.NewGuid().ToString();
        for (var i = 0; i < n; i++)
        {
            using var runTokens = tokensProvider();
            await runtimeEngine.Execute<LoopTestArgs>(
                request: new(
                    content: @$"X * 5", contentId: contentId, arguments: new LoopTestArgs { X = i },
                    imports: null, assemblies: null, types: null, tokens: runTokens
                ));
        }
    }

    public static async Task LoopJavascript(int n, IRuntimeEngine runtimeEngine, Func<RunTokens> tokensProvider)
    {
        IDisposable scope = null;
        Guid optimizationScopeId = Guid.NewGuid();
        string contentId = Guid.NewGuid().ToString();
        try
        {
            for (var i = 0; i < n; i++)
            {
                var runTokens = tokensProvider();
                scope = await runtimeEngine.Execute<LoopTestArgs>(
                    new(
                        content: @$"X * 5",
                        contentId: contentId,
                        arguments: new LoopTestArgs { X = i },
                        imports: null, assemblies: null, types: null,
                        tokens: runTokens, inputs: null,
                        isScriptOnly: true, useRawContent: true,
                        optimizationScopeId: optimizationScopeId
                    ));
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

    public static async Task TestJintLib(IRuntimeEngineFactory engineFactory, RunTokens runTokens)
    {
        using var _ = runTokens;
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        var result = await runtimeEngine.Execute<string, object>(
            request: new(
                content: @"
                    const testLodash = _.filter([1, 2, 3], item => !!item);
                    const apiString = await TestAsync();
                    return `Hello ` + testLodash.toString() + apiString;
                ",
                contentId: Guid.NewGuid().ToString(),
                arguments: new { TestAsync },
                imports: new[] { "import './lodash.min.js';" },
                assemblies: null, types: null, runTokens,
                inputs: new Dictionary<string, object> { [nameof(TestAsync)] = TestAsync }
            ));
    }

    public static async Task TestV8Lib(IRuntimeEngineFactory engineFactory, RunTokens runTokens)
    {
        using var _ = runTokens;
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        var result = await runtimeEngine.Execute<string, object>(
            request: new(
                content: @"
                    const testLodash = _.filter([1, 2, 3], item => !!item);
                    const apiString = await TestAsync();
                    return `Hello ` + testLodash.toString() + apiString;
                ",
                contentId: Guid.NewGuid().ToString(),
                arguments: new { TestAsync },
                imports: new[] { "import { fetch } from 'fetch.min.js'", "import 'lodash.min.js'", "import 'axios.min.js'" },
                assemblies: null, types: null, runTokens,
                inputs: new Dictionary<string, object> { [nameof(TestAsync)] = TestAsync }
            ));
    }
}

static class TestFunctionBlocks
{
    public static async Task BenchmarkComplexCFB(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        Console.WriteLine("=== Benchmark complex CFB ===");
        const int FirstLoop = 1;
        const int SecondLoop = 300;
        const int ThirdLoop = 700;
        var blockRunner = serviceProvider.GetRequiredService<IBlockRunner>();
        var engineFactory = serviceProvider.GetRequiredService<IRuntimeEngineFactory>();
        var functionRunner = serviceProvider.GetRequiredService<IFunctionRunner>();
        var blockFrameworkFactory = serviceProvider.GetRequiredService<IBlockFrameworkFactory>();
        var functionFramework = serviceProvider.GetRequiredService<AppFunctionFramework>();
        var taskRunner = serviceProvider.GetRequiredService<ISyncAsyncTaskRunner>();
        var taskLimiter = serviceProvider.GetRequiredService<ISyncAsyncTaskLimiter>();
        var jsEngine = engineFactory.CreateEngine(ERuntime.Javascript);
        var jsEngineName = jsEngine.GetType().Name;
        var csCompiledCFB = ComplexCFB.Build(
            bAddDef: PredefinedBFBs.AddCsCompiled,
            bMultiplyDef: PredefinedBFBs.MultiplyCsCompiled,
            bRandomDef: PredefinedBFBs.RandomCsCompiled,
            bDelayDef: PredefinedBFBs.DelayCsCompiled);
        IExecutionControl CreateControl(CompositeBlockDef blockDef) => new CompositeEC<AppFunctionFramework>(new(blockDef.Id), blockDef, blockRunner, functionRunner, blockFrameworkFactory, functionFramework, taskRunner, taskLimiter);

        Task<double> RunCsCompiled() => RunComplexCFB(
            blockRunner, CreateControl: () => CreateControl(blockDef: csCompiledCFB), runTokens: tokensProvider());

        var csScriptCFB = ComplexCFB.Build(
            bAddDef: PredefinedBFBs.AddCsScript,
            bMultiplyDef: PredefinedBFBs.MultiplyCsScript,
            bRandomDef: PredefinedBFBs.RandomCsScript,
            bDelayDef: PredefinedBFBs.DelayCsScript);
        Task<double> RunCsScript() => RunComplexCFB(
            blockRunner, CreateControl: () => CreateControl(blockDef: csScriptCFB), runTokens: tokensProvider());

        var jsCFB = ComplexCFB.Build(
            bAddDef: PredefinedBFBs.AddJs,
            bMultiplyDef: PredefinedBFBs.MultiplyJs,
            bRandomDef: PredefinedBFBs.RandomJs,
            bDelayDef: PredefinedBFBs.DelayJs);
        Task<double> RunJs() => RunComplexCFB(
            blockRunner, CreateControl: () => CreateControl(blockDef: jsCFB), runTokens: tokensProvider());

        var sw = Stopwatch.StartNew();
        await Loop(FirstLoop, task: RunCsCompiled);
        Console.WriteLine("C# compiled (1st): {0}", sw.ElapsedMilliseconds);
        await Loop(SecondLoop, task: RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, task: RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", ThirdLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await Loop(FirstLoop, task: RunCsScript);
        Console.WriteLine("C# script (1st): {0}", sw.ElapsedMilliseconds);
        await Loop(SecondLoop, task: RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, task: RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", ThirdLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await Loop(FirstLoop, task: RunJs);
        Console.WriteLine("{0} (1st): {1}", jsEngineName, sw.ElapsedMilliseconds);
        await Loop(SecondLoop, task: RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, SecondLoop, sw.ElapsedMilliseconds);
        await Loop(ThirdLoop, task: RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, ThirdLoop, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        const int ParallelLoopCount = 1000;
        Console.WriteLine();
        Console.WriteLine("=== Benchmark complex CFB (Parallel) ===");
        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunCsCompiled);
        Console.WriteLine("C# compiled ({0}): {1}", ParallelLoopCount, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunCsScript);
        Console.WriteLine("C# script ({0}): {1}", ParallelLoopCount, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();

        sw.Restart();
        await ParallelLoop(ParallelLoopCount, RunJs);
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, ParallelLoopCount, sw.ElapsedMilliseconds);
        Program.ForceGCCollect();
    }

    public static async Task Loop(int n, Func<Task> task)
    {
        for (int i = 0; i < n; i++)
            await task();
    }

    public static async Task ParallelLoop(int n, Func<Task> task)
    {
        var waits = new List<TaskCompletionSource>();
        for (int i = 0; i < n; i++)
        {
            var tcs = new TaskCompletionSource();
            waits.Add(tcs);
            Task parallelTask = null;
            parallelTask = Task.Factory.StartNew(function: async () =>
            {
                try
                {
                    await task();
                    tcs.SetResult();
                }
                catch (Exception ex) { tcs.SetException(ex); }
                finally { parallelTask.Dispose(); }
            }, creationOptions: TaskCreationOptions.LongRunning);
        }
        await Task.WhenAll(waits.Select(w => w.Task));
    }

    public static async Task RunEntryReport(DataStore dataStore,
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var temperatureEntry = dataStore.GetEntry("Temperature");
        var humidityEntry = dataStore.GetEntry("Humidity");
        var reportEntry = dataStore.GetEntry("Report");
        var finalReportEntry = dataStore.GetEntry("FinalReport");
        var bindings = new HashSet<VariableBinding>();
        using var execControl = CreateControl();

        var iTemp = execControl.GetVariable("Temperature", EVariableType.Input);
        var iHumidity = execControl.GetVariable("Humidity", EVariableType.Input);
        var iReport = execControl.GetVariable("Report", EVariableType.Input);
        var oReport = execControl.GetVariable("Report", EVariableType.Output);
        var oFinalReport = execControl.GetVariable("FinalReport", EVariableType.Output);

        var iTempRef = new EntryValueObject(iTemp, temperatureEntry);
        var iHumidityRef = new EntryValueObject(iHumidity, humidityEntry);
        var iReportRef = new EntryValueObject(iReport, reportEntry);
        var oReportRef = new EntryValueObject(oReport, reportEntry);
        var oFinalReportRef = new EntryValueObject(oFinalReport, finalReportEntry);

        bindings.Add(new(variableName: iTemp.Name, reference: iTempRef, type: EBindingType.Input));
        bindings.Add(new(variableName: iHumidity.Name, reference: iHumidityRef, type: EBindingType.Input));
        bindings.Add(new(variableName: iReport.Name, reference: iReportRef, type: EBindingType.Input));
        bindings.Add(new(variableName: oReport.Name, reference: oReportRef, type: EBindingType.Output));
        bindings.Add(new(variableName: oFinalReport.Name, reference: oFinalReportRef, type: EBindingType.Output));

        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("FinalReport") as EntryValueObject;
        dataStore.UpdateEntry(finalResult.EntryKey, finalResult.Value);
        finalReportEntry = dataStore.GetEntry(finalReportEntry.Key);
        Console.WriteLine("Result: {0} | {1}", finalResult, finalReportEntry);
    }

    public static async Task RunSampleMetric(DataStore dataStore,
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var metric = dataStore.GetMetricSnapshot(MetricSnapshot.SampleMetric);
        using var execControl = CreateControl();

        var iMetric = execControl.GetVariable("Metric", EVariableType.Input);
        var iMetricRef = new MetricValueObject(iMetric, metric);

        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: iMetric.Name, reference: iMetricRef, type: EBindingType.Input));

        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var snapshot = execControl.GetOutput("Snapshot");
        var previous = execControl.GetOutput("Previous");
        Console.WriteLine("Metric: {0} | Snapshot: {1} | Previous: {2}", metric.Metric, snapshot, previous);
    }

    public static async Task<double> RunComplexCFB(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Add1X", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1Y", value: 10, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        return (double)finalResult.Value;
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        var blockRunner = serviceProvider.GetRequiredService<IBlockRunner>();
        var engineFactory = serviceProvider.GetRequiredService<IRuntimeEngineFactory>();
        var functionRunner = serviceProvider.GetRequiredService<IFunctionRunner>();
        var blockFrameworkFactory = serviceProvider.GetRequiredService<IBlockFrameworkFactory>();
        var functionFramework = serviceProvider.GetRequiredService<AppFunctionFramework>();
        var taskRunner = serviceProvider.GetRequiredService<ISyncAsyncTaskRunner>();
        var taskLimiter = serviceProvider.GetRequiredService<ISyncAsyncTaskLimiter>();
        var dataStore = serviceProvider.GetService<DataStore>();
        const int DelayMs = 5000;
        ICompositeEC CreateCompositeControl(CompositeBlockDef blockDef) => new CompositeEC<AppFunctionFramework>(new(blockDef.Id), blockDef, blockRunner, functionRunner, blockFrameworkFactory, functionFramework, taskRunner, taskLimiter);
        IExecutionControl CreateBasicControl(BasicBlockDef blockDef) => new BasicEC<AppFunctionFramework>(block: new(blockDef.Id), blockDef, importBlocks: null, functionRunner, blockFrameworkFactory, functionFramework);

        Console.WriteLine("=== Test sample metric ===");
        await RunSampleMetric(dataStore, blockRunner, CreateControl: () => CreateCompositeControl(blockDef: SampleMetricCFB.Build()), runTokens: tokensProvider());

        await RunObjectAndFunctions(blockRunner, CreateControl: () => CreateCompositeControl(ObjectAndFunctionsCFB.Build()), runTokens: tokensProvider());

        await RunLogAndDebug(blockRunner, CreateControl: CreateCompositeControl, tokensProvider);

        Console.WriteLine("DelayJS {0} ms", DelayMs);
        await RunBlockDelay(
            blockRunner, CreateControl: () => CreateBasicControl(blockDef: PredefinedBFBs.DelayJs),
            delayMs: DelayMs, runTokens: tokensProvider());

        await RunBlockRandomDouble(blockRunner, CreateControl: () => CreateBasicControl(blockDef: PredefinedBFBs.RandomCsScript), runTokens: tokensProvider());

        await RunBlockFactorial(blockRunner, CreateControl: () => CreateBasicControl(blockDef: PredefinedBFBs.FactorialCsScript), runTokens: tokensProvider());

        var rectangleAreaCFB = RectangleAreaCFB.Build(
            bMultiplyDef: PredefinedBFBs.MultiplyCsScript
        );
        await RunRectangleArea(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: rectangleAreaCFB), runTokens: tokensProvider());

        await RunRectanglePerimeter(blockRunner,
            CreateControl: () => CreateCompositeControl(blockDef: RectanglePerimeterCFB.Build(
                bAddDef: PredefinedBFBs.AddCsScript, bMultiplyDef: PredefinedBFBs.MultiplyCsCompiled
            )),
            runTokens: tokensProvider());

        var rectanglePerimeterJs = RectanglePerimeterCFB.Build(
            bAddDef: PredefinedBFBs.AddJs, bMultiplyDef: PredefinedBFBs.MultiplyCsCompiled
        );
        await RunRectanglePerimeter(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: rectanglePerimeterJs), runTokens: tokensProvider());
        Console.WriteLine("\nCFB: Rectangle perimeter:\n{0}\n", JsonSerializer.Serialize(rectanglePerimeterJs, Program.DefaultJsonOpts));
        Console.WriteLine("\nBFB: Add:\n{0}\n", JsonSerializer.Serialize(PredefinedBFBs.AddJs, Program.DefaultJsonOpts));
        Console.WriteLine("\nBFB: Multiply:\n{0}\n", JsonSerializer.Serialize(PredefinedBFBs.MultiplyCsCompiled, Program.DefaultJsonOpts));

        await RunLoopCFB(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: LoopCFB.Build()), runTokens: tokensProvider());

        await RunDependencyWait(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: DependencyWaitCFB.Build()), runTokens: tokensProvider());

        Console.WriteLine("=== Test entry report ===");
        await RunEntryReport(dataStore, blockRunner, CreateControl: () => CreateCompositeControl(blockDef: EntryReportCFB.Build()), runTokens: tokensProvider());
    }

    public static async Task RunBlockDelay(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl,
        int delayMs, RunTokens runTokens)
    {
        using var _ = runTokens;
        using var execControl = CreateControl();
        var bindings = new VariableBinding[] { new("Ms", delayMs, type: EBindingType.Input) };
        var runRequest = new RunBlockRequest(bindings, runTokens, triggerEvent: null);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
        Console.WriteLine(string.Join(Environment.NewLine, execControl.Result.OutputEvents));
    }

    public static async Task RunBlockRandomDouble(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl,
        RunTokens runTokens)
    {
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings: Array.Empty<VariableBinding>(), runTokens, triggerEvent: null);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
        Console.WriteLine(string.Join(Environment.NewLine, execControl.Result.OutputEvents));
        Console.WriteLine(execControl.GetOutput("Result"));
    }

    public static async Task RunBlockFactorial(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl,
        RunTokens runTokens)
    {
        using var execControl = CreateControl();
        var bindings = new VariableBinding[] { new("N", 5, type: EBindingType.Input) };
        var runRequest = new RunBlockRequest(bindings, runTokens, triggerEvent: null);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
        Console.WriteLine(string.Join(Environment.NewLine, execControl.Result.OutputEvents));
        Console.WriteLine(execControl.GetOutput("Result"));
    }

    public static async Task RunRectangleArea(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Length", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Width", value: 2, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunObjectAndFunctions(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Input", value: new
        {
            X = 5,
            Y = 1,
            Z = new EntryEntity("Name", "Trung")
        }, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Output");
        Console.WriteLine(finalResult);
    }

    private static void HandleLogActivity(object o, EventArgs e) => (o as IExecutionControl)?.LogBlockActivity();
    private static void HandleLogActivity(object o, Exception e) => (o as IExecutionControl)?.LogBlockActivity();
    private static void HandleLogFailure(object o, Exception e) => (o as IExecutionControl)?.LogFailure(e);

    public static async Task RunLogAndDebug(IBlockRunner blockRunner, Func<CompositeBlockDef, ICompositeEC> CreateControl, Func<RunTokens> tokensProvider)
    {
        async Task TryRunBlock(IExecutionControl execControl)
        {
            try
            {
                using var runTokens = tokensProvider();
                var runRequest = new RunBlockRequest(bindings: Array.Empty<VariableBinding>(), tokens: runTokens);
                await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
            }
            catch { }
        }

        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.CompilationErrorJs));
            execControl.Failed += HandleLogFailure;
            try { await TryRunBlock(execControl); }
            finally { execControl.Failed -= HandleLogFailure; }
        }
        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.RuntimeExceptionJs));
            execControl.Failed += HandleLogFailure;
            try { await TryRunBlock(execControl); }
            finally { execControl.Failed -= HandleLogFailure; }
        }
        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.RuntimeExceptionJsFromCs));
            execControl.Failed += HandleLogFailure;
            try { await TryRunBlock(execControl); }
            finally { execControl.Failed -= HandleLogFailure; }
        }

        {
            var bindings = new HashSet<VariableBinding>();
            bindings.Add(new(variableName: "Add1X", value: 5, type: EBindingType.Input));
            bindings.Add(new(variableName: "Add1Y", value: 10, type: EBindingType.Input));
            bindings.Add(new(variableName: "DelayMs", value: 10000, type: EBindingType.Input));

            var jsCFB = ComplexCFB.Build(
                bAddDef: PredefinedBFBs.AddJs,
                bMultiplyDef: PredefinedBFBs.MultiplyJs,
                bRandomDef: PredefinedBFBs.RandomJs,
                bDelayDef: PredefinedBFBs.DelayInfiniteJs);
            using var execControl = CreateControl(jsCFB);
            execControl.Running += HandleLogActivity;
            execControl.Completed += HandleLogActivity;
            execControl.Failed += HandleLogActivity;
            execControl.ControlRunning += HandleLogActivity;
            execControl.ControlCompleted += HandleLogActivity;
            execControl.ControlFailed += HandleLogActivity;

            try
            {
                using var runTokens = tokensProvider();
                var runRequest = new RunBlockRequest(bindings, tokens: runTokens);
                await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
                var finalResult = execControl.GetOutput("Result");
                Console.WriteLine("Complex CFB result: {0}", finalResult);
            }
            catch { }
            finally
            {
                execControl.Running -= HandleLogActivity;
                execControl.Completed -= HandleLogActivity;
                execControl.Failed -= HandleLogActivity;
                execControl.ControlRunning -= HandleLogActivity;
                execControl.ControlCompleted -= HandleLogActivity;
                execControl.ControlFailed -= HandleLogActivity;
            }
        }
    }

    public static async Task RunRectanglePerimeter(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Length", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Width", value: 2, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunLoopCFB(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "N", value: 1000, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunDependencyWait(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        using var _ = runTokens;
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "DelayMs", value: 3000, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1X", value: 1, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1Y", value: 2, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add2X", value: 3, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add2Y", value: 4, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.Run(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }
}

static class Misc
{
    public static async Task TestInMemoryLock()
    {
        using var lockManager = new InMemoryLockManager();

        Task Run(string threadName)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using var @lock = lockManager.Acquire("TEST", expiry: TimeSpan.FromSeconds(7), timeout: TimeSpan.FromSeconds(10));
                    Console.WriteLine($"{threadName} acquired");
                    await Task.Delay(15000);
                    Console.WriteLine($"{threadName} completed");
                }
                catch (Exception e)
                when (
                    ((e as AggregateException)?.InnerException ?? e) is TaskCanceledException
                    || ((e as AggregateException)?.InnerException ?? e) is OperationCanceledException
                )
                {
                    Console.WriteLine($"{threadName} timed out");
                }
            });
        }

        var tasks = new Task[] { Run("T1"), Run("T2"), Run("T3") };
        foreach (var task in tasks)
        {
            try { await task; } catch { }
        }
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

    public static async Task StartInputThread(CancellationTokenSource terminationCts)
    {
        var thread = new Thread(() =>
        {
            Console.WriteLine("\nPress 'x' to terminate execution\n");
            char keyChar;
            do
            {
                keyChar = (char)Console.Read();
                if (keyChar == 'x')
                    terminationCts.Cancel();
            }
            while (keyChar != 'x');
        });
        thread.IsBackground = true;
        thread.Start();
        await Task.Delay(2000);
    }

    public static void ForceGCCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}