using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Engines.V8Javascript.Models;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Engines;

public class V8JavascriptEngine : IOptimizableRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private const long DefaultMaxEngineCacheCount = 1_000;
    private readonly MemoryCache _scriptCache;
    private readonly ManualResetEventSlim _engineCacheWait;
    private readonly ConcurrentDictionary<Guid, V8ScriptEngine> _engineCache;
    private readonly IOptions<V8Options> _v8Options;
    public V8JavascriptEngine(IOptions<V8Options> v8Options)
    {
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _scriptCache = new MemoryCache(cacheOption);
        _engineCache = new();
        _engineCacheWait = new(initialState: true);
        _v8Options = v8Options;
        // [TODO] add lib loading cache
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    public Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
        => Execute<TReturn, TArg>(content, arguments, imports, assemblies, types, optimizationScopeId: default, cancellationToken);

    public Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
        => Execute(content, arguments, imports, assemblies, types, optimizationScopeId: default, cancellationToken);

    private static void TryAddGlobalObject<TArg>(V8ScriptEngine engine, TArg arguments)
    {
        if (arguments == null) return;
        engine.AddHostObject(
            itemName: "A",
            flags: HostItemFlags.GlobalMembers,
            target: arguments);
    }

    private async Task<V8Script> GetScript(V8ScriptEngine engine, Guid optimizationScopeId, string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var documentInfo = new DocumentInfo(name: CacheKey) { Category = ModuleCategory.Standard };
        content = AddImports(content, imports);
        bool firstCompiled = false;
        var (script, cachedOptScopeId, cacheBytes) = _scriptCache.GetOrCreate(CacheKey, (entry) =>
        {
            entry.SetSize(CacheSize);
            var script = engine.Compile(documentInfo, code: content, cacheKind: V8CacheKind.Code, out var cacheBytes);
            firstCompiled = true;
            return (script, optimizationScopeId, cacheBytes);
        });
        if (cachedOptScopeId != default && cachedOptScopeId == optimizationScopeId) return script;
        if (firstCompiled) return script;
        script = engine.Compile(documentInfo: documentInfo, code: content, cacheKind: V8CacheKind.Code, cacheBytes: cacheBytes, out bool accepted);
        return script;
    }

    private async Task<(string CacheKey, long CacheSize)> GetScriptCacheEntry(
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
        var contentSizeInBytes = hashContent.Length * 2;
        return (Encoding.UTF8.GetString(hash), contentSizeInBytes);
    }

    private V8ScriptEngine PrepareV8Engine(Type[] types, Guid optimizationScopeId)
    {
        V8ScriptEngine CreateNewEngine()
        {
            var engine = new V8ScriptEngine(flags:
                V8ScriptEngineFlags.EnableTaskPromiseConversion |
                V8ScriptEngineFlags.EnableDateTimeConversion |
                V8ScriptEngineFlags.EnableValueTaskPromiseConversion
            );
            engine.DocumentSettings.SearchPath = _v8Options.Value.LibraryFolderPath;
            engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;
            if (types?.Any() == true)
                engine.AddHostTypes(types);
            return engine;
        }

        if (optimizationScopeId == default) return CreateNewEngine();
        lock (_engineCache)
        {
            _engineCacheWait.Wait();
            if (!_engineCache.TryGetValue(optimizationScopeId, out var engine))
            {
                engine = CreateNewEngine();
                _engineCache[optimizationScopeId] = engine;
                if (_engineCache.Count >= DefaultMaxEngineCacheCount) _engineCacheWait.Reset();
            }
            return engine;
        }
    }

    private string AddImports(string content, IEnumerable<string> imports)
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
    }

    public Task CompleteOptimizationScope(Guid id)
    {
        if (_engineCache.Remove(id, out var engine))
        {
            engine.Dispose();
            lock (_engineCache)
            {
                if (_engineCache.Count < DefaultMaxEngineCacheCount && !_engineCacheWait.IsSet)
                    _engineCacheWait.Set();
            }
        }
        return Task.CompletedTask;
    }

    public async Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid optimizationScopeId, CancellationToken cancellationToken)
    {
        var combinedTypes = ReflectionHelper.CombineTypes(assemblies, types);
        V8ScriptEngine engine = PrepareV8Engine(combinedTypes, optimizationScopeId);
        TryAddGlobalObject(engine, arguments);
        var script = await GetScript(engine, optimizationScopeId, content, imports, assemblies, cancellationToken);
        var result = (TReturn)engine.Evaluate(script);
        return result;
    }

    public async Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, Guid optimizationScopeId, CancellationToken cancellationToken)
    {
        var combinedTypes = ReflectionHelper.CombineTypes(assemblies, types);
        V8ScriptEngine engine = PrepareV8Engine(combinedTypes, optimizationScopeId);
        TryAddGlobalObject(engine, arguments);
        var script = await GetScript(engine, optimizationScopeId, content, imports, assemblies, cancellationToken);
        var result = engine.Evaluate(script);
        if (result is Task task) await task;
    }
}