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
using WELearning.DynamicCodeExecution.Models;

namespace WELearning.DynamicCodeExecution.Engines;

// [NOTE] out-dated, later updates will be on Jint
public class V8JavascriptEngine : IRuntimeEngine, IDisposable
{
    private const string ArgumentsVar = "args";
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private const long DefaultMaxEngineCacheCount = 1_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _scriptCache;
    private readonly ManualResetEventSlim _engineCacheWait;
    private readonly ConcurrentDictionary<Guid, OptimizationScope> _engineCache;
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
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    private static void TryAddArguments<TArg>(V8ScriptEngine engine, TArg arguments)
    {
        if (arguments == null) return;
        engine.AddHostObject(
            itemName: ArgumentsVar,
            flags: HostItemFlags.GlobalMembers,
            target: arguments);
    }

    private async Task<V8Script> GetScript(V8ScriptEngine engine, OptimizationScope scope, string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        var (ModuleName, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var documentInfo = new DocumentInfo(name: ModuleName) { Category = ModuleCategory.Standard };
        content = AddImports(content, imports);
        var moduleId = $"System_{ModuleName}";
        var wrappedCode = @$"
            import {{ {JsEngineConstants.ExportedFunctionName} }} from '{moduleId}';
            {JsEngineConstants.ExportedFunctionName}({ArgumentsVar})";

        var moduleLoaded = scope?.ModuleLoaded.Contains(ModuleName) == true;
        if (!moduleLoaded)
        {
            engine.DocumentSettings.AddSystemDocument(identifier: moduleId, contents: content, category: ModuleCategory.Standard);
            scope?.ModuleLoaded.Add(ModuleName);
        }

        bool compiled = false;
        var (script, cacheBytes) = _scriptCache.GetOrCreate(ModuleName, (entry) =>
        {
            entry.SetSize(CacheSize);
            entry.SetSlidingExpiration(DefaultSlidingExpiration);
            var script = engine.Compile(documentInfo, code: wrappedCode, cacheKind: V8CacheKind.Code, out var cacheBytes);
            compiled = true;
            return (script, cacheBytes);
        });

        if (compiled) return script;
        script = engine.Compile(documentInfo: documentInfo, code: wrappedCode, cacheKind: V8CacheKind.Code, cacheBytes: cacheBytes, out bool accepted);
        return script;
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

    private (V8ScriptEngine Engine, OptimizationScope Scope) PrepareV8Engine(Type[] types, Guid? optimizationScopeId, CancellationToken cancellationToken)
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

        if (optimizationScopeId == default) return (CreateNewEngine(), null);
        _engineCacheWait.Wait(cancellationToken);
        lock (_engineCache)
        {
            V8ScriptEngine engine;
            if (!_engineCache.TryGetValue(optimizationScopeId.Value, out var scope))
            {
                engine = CreateNewEngine();
                scope = new OptimizationScope(engine, optimizationScopeId.Value, RemoveCache: (scopeId) =>
                {
                    if (_engineCache.Remove(scopeId, out _))
                    {
                        lock (_engineCache)
                        {
                            if (_engineCache.Count < DefaultMaxEngineCacheCount && !_engineCacheWait.IsSet)
                                _engineCacheWait.Set();
                        }
                    }
                });
                _engineCache[optimizationScopeId.Value] = scope;
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
        foreach (var item in _engineCache.Values)
            item.Dispose();
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request)
    {
        var combinedTypes = ReflectionHelper.CombineTypes(request.Assemblies, request.Types);
        var (engine, optimizationScope) = PrepareV8Engine(combinedTypes, request.OptimizationScopeId, cancellationToken: request.Tokens.Combined);
        TryAddArguments(engine, request.Arguments);
        var script = await GetScript(engine, optimizationScope, request.Content, request.Imports, request.Assemblies, cancellationToken: request.Tokens.Combined);
        var evalResult = engine.Evaluate(script);
        TReturn result;
        if (evalResult is Task<object> task)
            result = (TReturn)await task;
        else
            result = (TReturn)evalResult;
        return (result, optimizationScope);
    }

    public async Task<IDisposable> Execute<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var combinedTypes = ReflectionHelper.CombineTypes(request.Assemblies, request.Types);
        var (engine, optimizationScope) = PrepareV8Engine(combinedTypes, request.OptimizationScopeId, cancellationToken: request.Tokens.Combined);
        TryAddArguments(engine, request.Arguments);
        var script = await GetScript(engine, optimizationScope, request.Content, request.Imports, request.Assemblies, cancellationToken: request.Tokens.Combined);
        var result = engine.Evaluate(script);
        if (result is Task task) await task;
        return optimizationScope;
    }

    class OptimizationScope : IDisposable
    {
        private readonly Action<Guid> RemoveCache;
        public OptimizationScope(
            V8ScriptEngine engine, Guid scopeId,
            Action<Guid> RemoveCache)
        {
            Engine = engine;
            Id = scopeId;
            this.RemoveCache = RemoveCache;
            ModuleLoaded = new HashSet<string>();
        }

        public Guid Id { get; }
        public V8ScriptEngine Engine { get; }
        public HashSet<string> ModuleLoaded { get; }

        public void Dispose()
        {
            Engine.Dispose();
            RemoveCache(Id);
        }
    }
}