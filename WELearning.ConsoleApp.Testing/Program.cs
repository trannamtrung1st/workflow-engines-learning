
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WELearning.ConsoleApp.Testing.CompositeBlocks;
using WELearning.ConsoleApp.Testing.Entities;
using WELearning.ConsoleApp.Testing.ValueObjects;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.Core.Reflection.Extensions;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Shared.Concurrency;
using WELearning.Shared.Concurrency.Extensions;

const string LibraryFolderPath = "/Users/trungtran/MyPlace/Personal/Learning/workflow-engines-learning/local/libs";
var serviceCollection = new ServiceCollection()
    .AddLogging(cfg => cfg.AddConsole())
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    // FunctionBlock services
    .AddDefaultBlockRunner()
    .AddDefaultFunctionRunner()
    .AddDefaultBlockFrameworkFactory()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddTransientFunctionFramework<AppFunctionFramework>()
    .AddCSharpCompiledEngine()
    .AddCSharpScriptEngine()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options => options.LibraryFolderPath = LibraryFolderPath)
    .AddV8JavascriptEngine(options => options.LibraryFolderPath = LibraryFolderPath);

using var rootServiceProvider = serviceCollection.BuildServiceProvider();
using var scope = rootServiceProvider.CreateScope();
var serviceProvider = scope.ServiceProvider;
var terminationCts = new CancellationTokenSource();
var tokensProvider = () =>
{
    var timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
    return new RunTokens(timeout: timeoutToken, termination: terminationCts.Token);
};

await StartInputThread(terminationCts);
await TestEngines.BenchmarkLoops(serviceProvider, tokensProvider);
Console.WriteLine();
await TestFunctionBlocks.BenchmarkComplexCFB(serviceProvider, tokensProvider);
Console.WriteLine();
await TestEngines.Run(serviceProvider, tokensProvider);
Console.WriteLine();
await TestFunctionBlocks.Run(serviceProvider, tokensProvider);

// === Definitions ===

static class TestEngines
{
    public static async Task BenchmarkLoops(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        Console.WriteLine("=== Benchmark loops ===");
        var runtimeEngineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
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
        // const int SecondLoop = 100_000;
        const int SecondLoop = 1_000_000;

        var sw = Stopwatch.StartNew();
        await LoopCSharpCompiled(FirstLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, runTokens: tokensProvider());
        Console.WriteLine("C# compiled (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpCompiled(SecondLoop, csCompiledEngine, imports: imports, assemblies: compiledAssemblies, runTokens: tokensProvider());
        Console.WriteLine("C# compiled ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await LoopCSharpScript(FirstLoop, csScriptEngine, runTokens: tokensProvider());
        Console.WriteLine("C# script (1st): {0}", sw.ElapsedMilliseconds);
        await LoopCSharpScript(SecondLoop, csScriptEngine, runTokens: tokensProvider());
        Console.WriteLine("C# script ({0}): {1}", SecondLoop, sw.ElapsedMilliseconds);

        sw.Restart();
        await LoopJavascript(FirstLoop, jsEngine, runTokens: tokensProvider());
        Console.WriteLine("{0} (1st): {1}", jsEngineName, sw.ElapsedMilliseconds);
        await LoopJavascript(SecondLoop, jsEngine, runTokens: tokensProvider());
        Console.WriteLine("{0} ({1}): {2}", jsEngineName, SecondLoop, sw.ElapsedMilliseconds);
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        var runtimeEngineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        // await TestV8Lib(runtimeEngineFactory, runTokens: tokensProvider());
        await TestJintLib(runtimeEngineFactory, runTokens: tokensProvider());
    }

    public static async Task LoopCSharpCompiled(int n, IRuntimeEngine runtimeEngine, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, RunTokens runTokens)
    {
        for (var i = 0; i < n; i++)
        {
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
                    arguments: new LoopTestArgs { X = i },
                    flattenArguments: null,
                    flattenOutputs: null,
                    imports: imports, assemblies: assemblies, types: null, runTokens
                ));
        }
    }

    public static async Task LoopCSharpScript(int n, IRuntimeEngine runtimeEngine, RunTokens runTokens)
    {
        for (var i = 0; i < n; i++)
        {
            await runtimeEngine.Execute<LoopTestArgs>(
                request: new(
                    content: @$"X * 5", arguments: new LoopTestArgs { X = i },
                    flattenArguments: null,
                    flattenOutputs: null,
                    imports: null, assemblies: null, types: null, runTokens
                ));
        }
    }

    public static async Task LoopJavascript(int n, IRuntimeEngine runtimeEngine, RunTokens runTokens)
    {
        IDisposable scope = null;
        Guid optimizationScopeId = Guid.NewGuid();
        try
        {
            for (var i = 0; i < n; i++)
            {
                scope = await runtimeEngine.Execute<LoopTestArgs>(
                    new(
                        content: @$"return X * 5",
                        arguments: new LoopTestArgs { X = i },
                        flattenArguments: new (string, object)[] { ("X", i) },
                        flattenOutputs: null,
                        imports: null, assemblies: null, types: null,
                        runTokens, optimizationScopeId
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
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        var result = await runtimeEngine.Execute<string, object>(
            request: new(
                content: @"
                    const testLodash = _.filter([1, 2, 3], item => !!item);
                    const apiString = await TestAsync();
                    return `Hello ` + testLodash.toString() + apiString;
                ",
                arguments: new { TestAsync },
                flattenArguments: new (string, object)[] { (nameof(TestAsync), TestAsync) },
                flattenOutputs: null,
                imports: new[] { "import './lodash.min.js';" },
                assemblies: null, types: null, runTokens
            ));
    }

    public static async Task TestV8Lib(IRuntimeEngineFactory engineFactory, RunTokens runTokens)
    {
        var runtimeEngine = engineFactory.CreateEngine(runtime: ERuntime.Javascript);
        var result = await runtimeEngine.Execute<string, object>(
            request: new(
                content: @"
                    const testLodash = _.filter([1, 2, 3], item => !!item);
                    const apiString = await TestAsync();
                    return `Hello ` + testLodash.toString() + apiString;
                ",
                arguments: new { TestAsync },
                flattenArguments: new (string, object)[] { (nameof(TestAsync), TestAsync) },
                flattenOutputs: null,
                imports: new[] { "import { fetch } from 'fetch.min.js'", "import 'lodash.min.js'", "import 'axios.min.js'" },
                assemblies: null, types: null, runTokens
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
        var blockRunner = serviceProvider.GetService<IBlockRunner>();
        var engineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        var functionRunner = serviceProvider.GetService<IFunctionRunner>();
        var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory>();
        var functionFramework = serviceProvider.GetService<AppFunctionFramework>();
        var jsEngine = engineFactory.CreateEngine(ERuntime.Javascript);
        var jsEngineName = jsEngine.GetType().Name;
        var csCompiledCFB = ComplexCFB.Build(
            bAddDef: PredefinedBFBs.AddCsCompiled,
            bMultiplyDef: PredefinedBFBs.MultiplyCsCompiled,
            bRandomDef: PredefinedBFBs.RandomCsCompiled,
            bDelayDef: PredefinedBFBs.DelayCsCompiled);
        IExecutionControl CreateControl(CompositeBlockDef blockDef) => new CompositeEC<AppFunctionFramework>(new(blockDef.Id), blockDef, blockRunner, functionRunner, blockFrameworkFactory, functionFramework);

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
        Console.WriteLine("=== Benchmark complex CFB (Parallel) ===");
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

    public static async Task RunEntryReport(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var dataStore = new DataStore();
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

        var iTempRef = new REntryValueObject(iTemp, temperatureEntry);
        var iHumidityRef = new REntryValueObject(iHumidity, humidityEntry);
        var iReportRef = new REntryValueObject(iReport, reportEntry);
        var oReportRef = new WEntryValueObject(oReport, reportEntry);
        var oFinalReportRef = new WEntryValueObject(oFinalReport, finalReportEntry);

        bindings.Add(new(variableName: iTemp.Name, reference: iTempRef, type: EBindingType.Input));
        bindings.Add(new(variableName: iHumidity.Name, reference: iHumidityRef, type: EBindingType.Input));
        bindings.Add(new(variableName: iReport.Name, reference: iReportRef, type: EBindingType.Input));
        bindings.Add(new(variableName: oReport.Name, reference: oReportRef, type: EBindingType.Output));
        bindings.Add(new(variableName: oFinalReport.Name, reference: oFinalReportRef, type: EBindingType.Output));

        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("FinalReport") as EntryValueObject;
        dataStore.UpdateEntry(finalResult.EntryKey, finalResult.Value);
        finalReportEntry = dataStore.GetEntry(finalReportEntry.Key);
        Console.WriteLine("Result: {0} | {1}", finalResult, finalReportEntry);
    }

    public static async Task<double> RunComplexCFB(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Add1X", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1Y", value: 10, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        return (double)finalResult.Value;
    }

    public static async Task Run(IServiceProvider serviceProvider, Func<RunTokens> tokensProvider)
    {
        var blockRunner = serviceProvider.GetService<IBlockRunner>();
        var engineFactory = serviceProvider.GetService<IRuntimeEngineFactory>();
        var functionRunner = serviceProvider.GetService<IFunctionRunner>();
        var blockFrameworkFactory = serviceProvider.GetService<IBlockFrameworkFactory>();
        var functionFramework = serviceProvider.GetService<AppFunctionFramework>();
        const int DelayMs = 5000;
        ICompositeEC CreateCompositeControl(CompositeBlockDef blockDef) => new CompositeEC<AppFunctionFramework>(new(blockDef.Id), blockDef, blockRunner, functionRunner, blockFrameworkFactory, functionFramework);
        IExecutionControl CreateBasicControl(BasicBlockDef blockDef) => new BasicEC<AppFunctionFramework>(block: new(blockDef.Id), blockDef, functionRunner, blockFrameworkFactory, functionFramework);

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
        Console.WriteLine("\n{0}\n", JsonSerializer.Serialize(rectanglePerimeterJs, Program.DefaultJsonOpts));

        await RunLoopCFB(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: LoopCFB.Build()), runTokens: tokensProvider());

        await RunDependencyWait(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: DependencyWaitCFB.Build()), runTokens: tokensProvider());

        Console.WriteLine("Test run entry report: ");
        await RunEntryReport(blockRunner, CreateControl: () => CreateCompositeControl(blockDef: EntryReportCFB.Build()), runTokens: tokensProvider());
    }

    public static async Task RunBlockDelay(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl,
        int delayMs, RunTokens runTokens)
    {
        using var execControl = CreateControl();
        var bindings = new VariableBinding[] { new("Ms", delayMs, type: EBindingType.Input) };
        var runRequest = new RunBlockRequest(bindings, runTokens, triggerEvent: null);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);
        Console.WriteLine(string.Join(Environment.NewLine, execControl.Result.OutputEvents));
    }

    public static async Task RunBlockRandomDouble(
        IBlockRunner blockRunner, Func<IExecutionControl> CreateControl,
        RunTokens runTokens)
    {
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings: Array.Empty<VariableBinding>(), runTokens, triggerEvent: null);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);
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
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);
        Console.WriteLine(string.Join(Environment.NewLine, execControl.Result.OutputEvents));
        Console.WriteLine(execControl.GetOutput("Result"));
    }

    public static async Task RunRectangleArea(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Length", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Width", value: 2, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunObjectAndFunctions(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Input", value: new
        {
            X = 5,
            Y = 1,
            Z = new EntryEntity("Name", "Trung")
        }, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Output");
        Console.WriteLine(finalResult);
    }

    public static async Task RunLogAndDebug(IBlockRunner blockRunner, Func<CompositeBlockDef, ICompositeEC> CreateControl, Func<RunTokens> tokensProvider)
    {
        async Task TryRunBlock(IExecutionControl execControl)
        {
            try
            {
                var runRequest = new RunBlockRequest(bindings: Array.Empty<VariableBinding>(), tokens: tokensProvider());
                await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);
            }
            catch { }
        }

        void LogFailure(object o, Exception ex)
        {
            if (o is not IExecutionControl control) return;
            string messageFormat =
@"=== {0} ===
+ Block: {1}
+ Function: {2}
+ Description: {3}
+ Location (line, column, index): ({4}, {5}, {6})
+ Source: {7}

Original content (error located):
-----------------";
            if (ex is FunctionCompilationError error)
            {
                var compErr = error.Error;
                Console.Error.WriteLine(format: messageFormat,
                    "Compilation error",
                    (control as ICompositeEC)?.ExceptionFrom.Block.Id ?? control.Block.Id,
                    (control as IBasicEC)?.RunningFunction?.Id,
                    compErr.Description,
                    compErr.LineNumber,
                    compErr.Column,
                    compErr.Index,
                    compErr.Source);
                error.PrintError();
                Console.WriteLine();
            }
            else if (ex is FunctionRuntimeException runtimeEx)
            {
                var exception = runtimeEx.Exception;
                Console.Error.WriteLine(format: messageFormat,
                    $"Runtime exception ({exception.Source})",
                    (control as ICompositeEC)?.ExceptionFrom.Block.Id ?? control.Block.Id,
                    (control as IBasicEC)?.RunningFunction?.Id,
                    exception.Description,
                    exception.LineNumber,
                    exception.Column,
                    exception.Index,
                    exception.Source);
                runtimeEx.PrintError();
                Console.WriteLine();
            }
            else
            {
                Console.Error.WriteLine(format: messageFormat,
                    $"System exception",
                    (control as ICompositeEC)?.ExceptionFrom.Block.Id ?? control.Block.Id,
                    (control as IBasicEC)?.RunningFunction?.Id,
                    ex.Message, -1, -1, -1, ex.Source);
                Console.WriteLine();
            }
        }

        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.CompilationErrorJs));
            execControl.Failed += LogFailure;
            await TryRunBlock(execControl);
        }
        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.RuntimeExceptionJs));
            execControl.Failed += LogFailure;
            await TryRunBlock(execControl);
        }
        {
            using var execControl = CreateControl(SimpleCFB.Build(bSimpleDef: PredefinedBFBs.RuntimeExceptionJsFromCs));
            execControl.Failed += LogFailure;
            await TryRunBlock(execControl);
        }

        {
            void LogBlockActivity(object o)
            {
                if (o is not IExecutionControl execControl) return;
                var lastActivity = execControl.LastActivity;
                string messageFormat =
@"
=== {0} ===
+ Run ID: {1}
+ Time (UTC): {2}
+ Status: {3}
+ Run time (ms): {4}
+ Exception from block: {5}
";
                Console.WriteLine(format: messageFormat,
                    $"{(lastActivity.Control is ICompositeEC ? "CFB" : "BFB")}: {lastActivity.Control.Block.Id}",
                    lastActivity.RunRequest.RunId,
                    lastActivity.TimeUtc,
                    lastActivity.Status,
                    lastActivity.RunTime?.TotalMilliseconds.ToString() ?? "N/A",
                    lastActivity.ExceptionFrom?.Block.Id ?? "N/A");
                if (lastActivity.Status == EBlockExecutionStatus.Failed)
                    LogFailure(o, lastActivity.Exception);
            }

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
            execControl.Running += (o, e) => LogBlockActivity(o);
            execControl.Completed += (o, e) => LogBlockActivity(o);
            execControl.Failed += (o, e) => LogBlockActivity(o);
            execControl.ControlRunning += (o, e) => LogBlockActivity(o);
            execControl.ControlCompleted += (o, e) => LogBlockActivity(o);
            execControl.ControlFailed += (o, e) => LogBlockActivity(o);

            try
            {
                var runRequest = new RunBlockRequest(bindings, tokens: tokensProvider());
                await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);
                var finalResult = execControl.GetOutput("Result");
                Console.WriteLine("Complex CFB result: {0}", finalResult);
            }
            catch { }
        }
    }

    public static async Task RunRectanglePerimeter(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "Length", value: 5, type: EBindingType.Input));
        bindings.Add(new(variableName: "Width", value: 2, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunLoopCFB(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "N", value: 1000, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

        var finalResult = execControl.GetOutput("Result");
        Console.WriteLine(finalResult);
    }

    public static async Task RunDependencyWait(IBlockRunner blockRunner, Func<IExecutionControl> CreateControl, RunTokens runTokens)
    {
        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: "DelayMs", value: 3000, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1X", value: 1, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add1Y", value: 2, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add2X", value: 3, type: EBindingType.Input));
        bindings.Add(new(variableName: "Add2Y", value: 4, type: EBindingType.Input));
        using var execControl = CreateControl();
        var runRequest = new RunBlockRequest(bindings, runTokens);
        await blockRunner.RunAndWait(runRequest, execControl, optimizationScopeId: default);

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
}