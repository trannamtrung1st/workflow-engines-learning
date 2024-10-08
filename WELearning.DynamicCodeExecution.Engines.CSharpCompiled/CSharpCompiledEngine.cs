using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Caching.Memory;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Helpers;
using WELearning.DynamicCodeExecution.Models;
using TNT.Boilerplates.Concurrency.Abstracts;

namespace WELearning.DynamicCodeExecution.Engines;

public class CSharpCompiledEngine : IRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _assemblyCache;
    private readonly IInMemoryLockManager _lockManager;
    public CSharpCompiledEngine(IInMemoryLockManager lockManager)
    {
        _lockManager = lockManager;
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _assemblyCache = new MemoryCache(cacheOption);
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.CSharpCompiled;

    public async Task<(TReturn Result, IOptimizationScope OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request)
    {
        var assemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types);
        var assembly = await LoadOrCompile(request.Content, request.Imports, assemblies, cancellationToken: request.Tokens.Combined);
        var baseType = typeof(IExecutable<TReturn, TArg>);
        var execType = assembly.ExportedTypes.FirstOrDefault(t => t.IsClass && baseType.IsAssignableFrom(t));
        var instance = (IExecutable<TReturn, TArg>)Activator.CreateInstance(execType);
        var result = await instance.Execute(request.Arguments, cancellationToken: request.Tokens.Combined);
        return (result, default);
    }

    public async Task<IOptimizationScope> Execute<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var assemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types);
        var assembly = await LoadOrCompile(request.Content, request.Imports, assemblies, cancellationToken: request.Tokens.Combined);
        var execType = assembly.ExportedTypes.FirstOrDefault(t => t.IsClass && typeof(IExecutable<TArg>).IsAssignableFrom(t));
        var instance = (IExecutable<TArg>)Activator.CreateInstance(execType);
        await instance.Execute(request.Arguments, cancellationToken: request.Tokens.Combined);
        return default;
    }

    private string AddImports(string content, IEnumerable<string> imports)
    {
        var nl = Environment.NewLine;
        var importPart = string.Empty;
        if (imports?.Any() == true)
            importPart = string.Join(nl, imports.Select(ns => $"using {ns};")) + nl;
        return importPart + content;
    }

    private static async Task<(string AssemblyName, long CacheSize)> GetScriptCacheEntry(
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
        var assemblyName = Convert.ToBase64String(hash);
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '+', '=' }))
            assemblyName = assemblyName.Replace(c, '_');
        var contentSizeInBytes = hashContent.Length * 2;
        return (assemblyName, contentSizeInBytes);
    }

    private async Task<Assembly> LoadOrCompile(string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        var (AssemblyName, CacheSize) = await GetScriptCacheEntry(content, imports, assemblies, cancellationToken);
        return _lockManager.MutexAccess(AssemblyName,
            task: () => _assemblyCache.GetOrCreate(AssemblyName, (entry) =>
            {
                entry.SetSize(CacheSize);
                entry.SetSlidingExpiration(DefaultSlidingExpiration);
                content = AddImports(content, imports);
                try
                {
                    var assembly = Assembly.Load(AssemblyName);
                    return assembly;
                }
                catch
                {
                    var syntaxTree = SyntaxFactory.ParseSyntaxTree(content);
                    var compilation = CSharpCompilation.Create(AssemblyName)
                        .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                        .AddSyntaxTrees(syntaxTree);
                    if (assemblies?.Any() == true)
                    {
                        var preloadAssemblies = assemblies.Select(dll => MetadataReference.CreateFromFile(dll.Location) as MetadataReference);
                        compilation = compilation.WithReferences(preloadAssemblies);
                    }
                    return EmitToMemory(compilation, cancellationToken);
                }
            }));
    }

    private static Assembly EmitToMemory(CSharpCompilation compilation, CancellationToken cancellationToken)
    {
        // [OPT] add file emit
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream, cancellationToken: cancellationToken);
        if (emitResult.Success)
            return Assembly.Load(peStream.ToArray(), pdbStream.ToArray());
        throw new Exception(string.Join("\n", emitResult.Diagnostics.ToList().Select(x => x.ToString())));
    }

    public void Dispose()
    {
        _assemblyCache.Dispose();
    }

    public Task<IOptimizationScope> Compile(CompileCodeRequest request)
    {
        throw new NotImplementedException();
    }
}