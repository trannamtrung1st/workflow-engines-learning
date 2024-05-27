using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Engines.V8Javascript.Models;

namespace WELearning.DynamicCodeExecution.Engines;

public class V8JavascriptEngine : IRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private readonly MemoryCache _memoryCache;
    private readonly V8Runtime _v8Runtime;
    public V8JavascriptEngine(IOptions<V8Options> v8Options)
    {
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _memoryCache = new MemoryCache(cacheOption);
        _v8Runtime = new V8Runtime();
        _v8Runtime.DocumentSettings.SearchPath = v8Options.Value.LibraryFolderPath;
        _v8Runtime.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;
        // [TODO] add lib loading cache
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    public async Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        using V8ScriptEngine engine = PrepareV8Engine(assemblies);
        AddGlobalObject(engine, arguments);
        var script = await GetScript(content, imports, assemblies, cancellationToken);
        var result = (TReturn)engine.Evaluate(script);
        return result;
    }

    public async Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        using V8ScriptEngine engine = PrepareV8Engine(assemblies);
        AddGlobalObject(engine, arguments);
        var script = await GetScript(content, imports, assemblies, cancellationToken);
        var result = engine.Evaluate(script);
        if (result is Task task) await task;
    }

    private void AddGlobalObject<TArg>(V8ScriptEngine engine, TArg arguments)
    {
        var type = arguments.GetType();
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
            engine.AddHostObject(
                itemName: property.Name,
                flags: HostItemFlags.GlobalMembers,
                target: property.GetValue(arguments));
    }

    private async Task<V8Script> GetScript(string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var script = _memoryCache.GetOrCreate(CacheKey, (entry) =>
        {
            content = AddImports(content, imports);
            entry.SetSize(CacheSize);
            var v8Script = _v8Runtime.Compile(documentName: CacheKey, code: content);
            return v8Script;
        });
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

    private V8ScriptEngine PrepareV8Engine(IEnumerable<Assembly> assemblies)
    {
        var engine = _v8Runtime.CreateScriptEngine(flags:
            V8ScriptEngineFlags.EnableTaskPromiseConversion |
            V8ScriptEngineFlags.EnableDateTimeConversion |
            V8ScriptEngineFlags.EnableValueTaskPromiseConversion
        );
        if (assemblies?.Any() == true)
            engine.AddHostTypes(assemblies.SelectMany(ass => ass.ExportedTypes).ToArray());
        return engine;
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
        _memoryCache.Dispose();
        _v8Runtime.Dispose();
    }
}