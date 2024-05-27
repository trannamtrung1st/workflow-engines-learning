using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Caching.Memory;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;

namespace WELearning.DynamicCodeExecution.Engines;

public class CSharpCompiledEngine : IRuntimeEngine, IDisposable
{
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private readonly MemoryCache _memoryCache;
    public CSharpCompiledEngine()
    {
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _memoryCache = new MemoryCache(cacheOption);
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.CSharpCompiled;

    public async Task<TReturn> Execute<TReturn, TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        var assembly = await LoadOrCompile(content, imports, assemblies, cancellationToken);
        var execType = assembly.ExportedTypes.FirstOrDefault(t => t.IsClass && typeof(IExecutable<TReturn, TArg>).IsAssignableFrom(t));
        var instance = (IExecutable<TReturn, TArg>)Activator.CreateInstance(execType);
        return await instance.Execute(arguments, cancellationToken);
    }

    public async Task Execute<TArg>(string content, TArg arguments, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        var assembly = await LoadOrCompile(content, imports, assemblies, cancellationToken);
        var execType = assembly.ExportedTypes.FirstOrDefault(t => t.IsClass && typeof(IExecutable<TArg>).IsAssignableFrom(t));
        var instance = (IExecutable<TArg>)Activator.CreateInstance(execType);
        await instance.Execute(arguments, cancellationToken);
    }

    private string AddImports(string content, IEnumerable<string> imports)
    {
        var nl = Environment.NewLine;
        var importPart = string.Empty;
        if (imports?.Any() == true)
            importPart = string.Join(nl, imports.Select(ns => $"using {ns};")) + nl;
        return importPart + content;
    }

    private async Task<(string AssemblyName, long CacheSize)> GetScriptCacheEntry(
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
        var assembly = _memoryCache.GetOrCreate(AssemblyName, (entry) =>
        {
            entry.SetSize(CacheSize);
            content = AddImports(content, imports);
            try
            {
                var assembly = Assembly.Load(AssemblyName);
                return assembly;
            }
            catch
            {
                var syntaxTree = SyntaxFactory.ParseSyntaxTree(content);
                var preloadAssemblies = assemblies.Select(dll => MetadataReference.CreateFromFile(dll.Location) as MetadataReference);
                var compilation = CSharpCompilation.Create(AssemblyName)
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .WithReferences(preloadAssemblies)
                    .AddSyntaxTrees(syntaxTree);
                return EmitToMemory(compilation, cancellationToken);
            }
        });
        return assembly;
    }

    private Assembly EmitToMemory(CSharpCompilation compilation, CancellationToken cancellationToken)
    {
        // [TODO] add file emit
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream, pdbStream, cancellationToken: cancellationToken);
        if (emitResult.Success)
            return Assembly.Load(peStream.ToArray(), pdbStream.ToArray());
        throw new Exception(string.Join("\n", emitResult.Diagnostics.ToList().Select(x => x.ToString())));
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}