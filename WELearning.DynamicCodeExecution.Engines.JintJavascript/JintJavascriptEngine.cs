using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Engines.JintJavascript.Models;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Engines;

public class JintJavascriptEngine : IOptimizableRuntimeEngine, IDisposable
{
    private const string ExportedFunctionName = nameof(IExecutable<object>.Execute);
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private const long DefaultMaxEngineCacheCount = 1_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _scriptCache;
    private readonly ManualResetEventSlim _engineCacheWait;
    private readonly ConcurrentDictionary<Guid, OptimizationScope> _engineCache;
    private readonly IOptions<JintOptions> _jintOptions;
    public JintJavascriptEngine(IOptions<JintOptions> JintOptions)
    {
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _scriptCache = new MemoryCache(cacheOption);
        _engineCache = new();
        _engineCacheWait = new(initialState: true);
        _jintOptions = JintOptions;
        // [TODO] add lib loading cache
        // [TODO] lock
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    public async Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
    {
        var (result, _) = await Execute<TReturn, TArg>(content, arguments, imports, assemblies, types, optimizationScopeId: default, cancellationToken);
        return result;
    }

    public async Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
        => await Execute(content, arguments, imports, assemblies, types, optimizationScopeId: default, cancellationToken);

    private async Task<ObjectInstance> GetScriptModule(Engine engine, string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        var (ModuleName, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var module = _scriptCache.GetOrCreate(ModuleName, (entry) =>
        {
            entry.SetSize(CacheSize);
            entry.SetSlidingExpiration(DefaultSlidingExpiration);
            content = AddImports(content, imports);
            var preparedModule = Engine.PrepareModule(code: content, options: new ModulePreparationOptions
            {
                ParsingOptions = ModuleParsingOptions.Default
            });
            engine.Modules.Add(ModuleName, b => b.AddModule(preparedModule));
            var module = engine.Modules.Import(ModuleName);
            return module;
        });
        return module;
    }

    private static async Task<(string ModuleName, long CacheSize)> GetScriptCacheEntry(
        string content, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies,
        CancellationToken cancellationToken)
    {
        using var md5 = MD5.Create();
        var importsStr = imports != null ? string.Join(string.Empty, imports) : null;
        var assembliesStr = assemblies != null ? string.Join(string.Empty, assemblies.Select(ass => ass.FullName)) : null;
        var hashContent = $"{content}_{importsStr}_{assembliesStr}";
        using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(hashContent));
        var hash = await md5.ComputeHashAsync(memStream, cancellationToken);
        var moduleName = Convert.ToBase64String(hash);
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '+', '=' }))
            moduleName = moduleName.Replace(c, '_');
        var contentSizeInBytes = hashContent.Length * 2;
        return (moduleName, contentSizeInBytes);
    }

    private (Engine Engine, OptimizationScope Scope) PrepareEngine(Assembly[] assemblies, IEnumerable<Type> types, Guid? optimizationScopeId)
    {
        Engine CreateNewEngine()
        {
            var engine = new Engine(options: cfg =>
            {
                cfg.EnableModules(basePath: _jintOptions.Value.LibraryFolderPath, restrictToBasePath: true);
                if (assemblies?.Any() == true)
                    cfg.AllowClr(assemblies);
            });
            if (types?.Any() == true)
            {
                foreach (var type in types)
                    engine.SetValue(type.Name, type);
            }
            return engine;
        }

        if (optimizationScopeId == default) return (CreateNewEngine(), null);
        lock (_engineCache)
        {
            _engineCacheWait.Wait();
            Engine engine;
            if (!_engineCache.TryGetValue(optimizationScopeId.Value, out var scope))
            {
                engine = CreateNewEngine();
                _engineCache[optimizationScopeId.Value] = new OptimizationScope(engine, optimizationScopeId.Value, _engineCache, _engineCacheWait);
                if (_engineCache.Count >= DefaultMaxEngineCacheCount) _engineCacheWait.Reset();
            }
            else
                engine = scope.Engine;
            return (engine, scope);
        }
    }

    private static string AddImports(string content, IEnumerable<string> imports)
    {
        var nl = Environment.NewLine;
        var importPart = string.Empty;
        if (imports?.Any() == true)
            importPart = string.Join(nl, imports) + nl;
        return importPart + content;
    }

    public void Dispose()
    {
        _scriptCache.Dispose();
        _engineCacheWait.Dispose();
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var combinedAssemblies = ReflectionHelper.CombineAssemblies(assemblies, types)?.ToArray();
        var (engine, optimizationScope) = PrepareEngine(combinedAssemblies, types, optimizationScopeId);
        var module = await GetScriptModule(engine, content, imports, assemblies, cancellationToken);
        var exportedFunction = module.Get(ExportedFunctionName);
        var result = exportedFunction.Call(arg1: JsValue.FromObjectWithType(engine, arguments, typeof(TArg)));
        var castResult = Cast<TReturn>(result.UnwrapIfPromise());
        return (castResult, optimizationScope);
    }

    public async Task<IDisposable> Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        var combinedAssemblies = ReflectionHelper.CombineAssemblies(assemblies, types)?.ToArray();
        var (engine, optimizationScope) = PrepareEngine(combinedAssemblies, types, optimizationScopeId);
        var module = await GetScriptModule(engine, content, imports, assemblies, cancellationToken);
        var exportedFunction = module.Get(ExportedFunctionName);
        var result = exportedFunction.Call(arg1: JsValue.FromObjectWithType(engine, arguments, typeof(TArg)));
        result.UnwrapIfPromise();
        return optimizationScope;
    }

    private static TReturn Cast<TReturn>(JsValue jsValue)
    {
        object value;
        // [TODO] cast
        switch (jsValue.Type)
        {
            case Jint.Runtime.Types.Boolean: value = jsValue.AsBoolean(); break;
            case Jint.Runtime.Types.Number: value = jsValue.AsNumber(); break;
            case Jint.Runtime.Types.Object: value = jsValue.ToObject(); break;
            case Jint.Runtime.Types.String: value = jsValue.AsString(); break;
            case Jint.Runtime.Types.Empty:
            case Jint.Runtime.Types.Undefined:
            case Jint.Runtime.Types.Null: return default;
            default: throw new NotSupportedException($"{jsValue.Type}");
        }
        return (TReturn)value;
    }

    class OptimizationScope : IDisposable
    {
        private readonly ManualResetEventSlim _engineCacheWait;
        private readonly ConcurrentDictionary<Guid, OptimizationScope> _engineCache;
        private readonly Guid _scopeId;
        public OptimizationScope(
            Engine engine, Guid scopeId,
            ConcurrentDictionary<Guid, OptimizationScope> engineCache,
            ManualResetEventSlim engineCacheWait)
        {
            Engine = engine;
            _scopeId = scopeId;
            _engineCache = engineCache;
            _engineCacheWait = engineCacheWait;
        }

        public Engine Engine { get; }

        public void Dispose()
        {
            Engine.Dispose();
            if (_engineCache.Remove(_scopeId, out _))
            {
                lock (_engineCache)
                {
                    if (_engineCache.Count < DefaultMaxEngineCacheCount && !_engineCacheWait.IsSet)
                        _engineCacheWait.Set();
                }
            }
        }
    }
}