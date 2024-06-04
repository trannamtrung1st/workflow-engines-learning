using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Caching.Memory;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Helpers;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.DynamicCodeExecution.Engines;

public class CSharpScriptEngine : IRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _scriptCache;
    private readonly IInMemoryLockManager _lockManager;
    public CSharpScriptEngine(IInMemoryLockManager lockManager)
    {
        _lockManager = lockManager;
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _scriptCache = new MemoryCache(cacheOption);
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.CSharpScript;

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request)
    {
        var assemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types);
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(request.Content, request.Imports, assemblies, cancellationToken: request.Tokens.Combined);

        Script<TReturn> script = null;
        _lockManager.MutexAccess(CacheKey, () =>
        {
            script = _scriptCache.GetOrCreate(CacheKey, (entry) =>
            {
                ConfigureCacheEntry(entry, CacheSize);
                var scriptOptions = PrepareScriptOptions(request.Imports, assemblies);
                var script = CSharpScript.Create<TReturn>(request.Content, scriptOptions, globalsType: typeof(TArg));
                script.Compile(cancellationToken: request.Tokens.Combined);
                return script;
            });
        });

        var result = await script.RunAsync(globals: request.Arguments, cancellationToken: request.Tokens.Combined);
        return (result.ReturnValue, default);
    }

    public async Task<IDisposable> Execute<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var assemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types);
        var (CacheKey, CacheSize) = await GetScriptCacheEntry(request.Content, request.Imports, assemblies, cancellationToken: request.Tokens.Combined);

        Script script = null;
        _lockManager.MutexAccess(CacheKey, () =>
        {
            script = _scriptCache.GetOrCreate(CacheKey, (entry) =>
            {
                ConfigureCacheEntry(entry, CacheSize);
                var scriptOptions = PrepareScriptOptions(request.Imports, assemblies);
                var script = CSharpScript.Create(request.Content, scriptOptions, globalsType: typeof(TArg));
                script.Compile(cancellationToken: request.Tokens.Combined);
                return script;
            });
        });

        var result = await script.RunAsync(globals: request.Arguments, cancellationToken: request.Tokens.Combined);
        return default;
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
        var options = ScriptOptions.Default
            .WithEmitDebugInformation(false)
            .WithOptimizationLevel(Microsoft.CodeAnalysis.OptimizationLevel.Release);
        if (imports?.Any() == true) options = options.WithImports(imports);
        if (assemblies?.Any() == true) options = options.WithReferences(assemblies);
        return options;
    }

    private static void ConfigureCacheEntry(ICacheEntry cacheEntry, long cacheSize)
    {
        cacheEntry.SetSize(cacheSize);
        cacheEntry.SetSlidingExpiration(DefaultSlidingExpiration);
    }

    public void Dispose()
    {
        _scriptCache.Dispose();
    }
}