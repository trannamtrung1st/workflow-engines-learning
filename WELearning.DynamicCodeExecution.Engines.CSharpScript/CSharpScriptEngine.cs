using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Caching.Memory;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Helpers;

namespace WELearning.DynamicCodeExecution.Engines;

public class CSharpScriptEngine : IRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private readonly MemoryCache _memoryCache;
    public CSharpScriptEngine()
    {
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _memoryCache = new MemoryCache(cacheOption);
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.CSharpScript;

    public async Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
    {
        assemblies = ReflectionHelper.CombineAssemblies(assemblies, types);
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var script = _memoryCache.GetOrCreate(CacheKey, (entry) =>
        {
            entry.SetSize(CacheSize);
            var scriptOptions = PrepareScriptOptions(imports, assemblies);
            var script = CSharpScript.Create<TReturn>(content, scriptOptions, globalsType: typeof(TArg));
            script.Compile(cancellationToken);
            return script;
        });
        var result = await script.RunAsync(globals: arguments, cancellationToken: cancellationToken);
        return result.ReturnValue;
    }

    public async Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, IEnumerable<Type> types, CancellationToken cancellationToken)
    {
        assemblies = ReflectionHelper.CombineAssemblies(assemblies, types);
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        var script = _memoryCache.GetOrCreate(CacheKey, (entry) =>
        {
            entry.SetSize(CacheSize);
            var scriptOptions = PrepareScriptOptions(imports, assemblies);
            var script = CSharpScript.Create(content, scriptOptions, globalsType: typeof(TArg));
            script.Compile(cancellationToken);
            return script;
        });
        var result = await script.RunAsync(globals: arguments, cancellationToken: cancellationToken);
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

    private ScriptOptions PrepareScriptOptions(IEnumerable<string> imports, IEnumerable<Assembly> assemblies)
    {
        var options = ScriptOptions.Default;
        if (imports?.Any() == true) options = options.WithImports(imports);
        if (assemblies?.Any() == true) options = options.WithReferences(assemblies);
        return options;
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}