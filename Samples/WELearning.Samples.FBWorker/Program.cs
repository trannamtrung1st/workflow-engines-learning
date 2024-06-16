using WELearning.Core.FunctionBlocks.Extensions;
using WELearning.Shared.Concurrency.Extensions;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.Core.Reflection.Extensions;
using WELearning.Shared.Diagnostic.Extensions;
using WELearning.Shared.Diagnostic.Abstracts;
using WELearning.Samples.FBWorker;
using WELearning.Samples.FBWorker.Configurations;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Shared.Diagnostic;
using WELearning.Samples.FBWorker.Services;
using WELearning.Samples.FBWorker.FunctionBlock;

const int minThreads = 512;
int maxThreads = minThreads * 2;
var threadSet = ThreadPool.SetMinThreads(workerThreads: minThreads, completionPortThreads: minThreads);
threadSet = ThreadPool.SetMaxThreads(workerThreads: maxThreads, completionPortThreads: maxThreads);

var builder = Host.CreateApplicationBuilder(args);
var appSettingsConfig = builder.Configuration.GetSection("AppSettings");
var taskLimiterConfig = builder.Configuration.GetSection("TaskLimiter");

builder.Services
    .AddHostedService<Worker>()
    .Configure<AppSettings>(appSettings => appSettingsConfig.Bind(appSettings))
    .AddLogging(cfg =>
    {
        cfg.ClearProviders();
        cfg.AddSimpleConsole();
    })
    .AddSingleton<IFunctionBlockWorker, FunctionBlockWorker>()
    .AddSingleton<IRateMonitor, RateMonitor>()
    .AddScoped<IFunctionBlockService, FunctionBlockService>()
    .AddScoped<IAssetService, AssetService>()
    .AddResourceMonitor()
    .AddFuzzyThreadController()
    .AddInMemoryLockManager()
    .AddDefaultDistributedLockManager()
    .AddDefaultSyncAsyncTaskRunner(configure: (options) => taskLimiterConfig.Bind(options))
    .AddDefaultBlockRunner()
    .AddDefaultFunctionRunner()
    .AddDefaultRuntimeEngineFactory()
    .AddDefaultTypeProvider()
    .AddBlockFrameworkFactory<DeviceBlockFrameworkFactory>()
    .AddFunctionFramework<DeviceFunctionFramework>()
    // For JS engines, first found engine will be used
    .AddJintJavascriptEngine(options =>
    {
        var libraryFolderPath = builder.Configuration["FunctionBlock:JavascriptEngine:LibraryFolderPath"];
        options.LibraryFolderPath = libraryFolderPath;
    });

var host = builder.Build();

host.Run();
