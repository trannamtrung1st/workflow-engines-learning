using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Esprima;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Debugger;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WELearning.DynamicCodeExecution.Abstracts;
using WELearning.DynamicCodeExecution.Constants;
using WELearning.DynamicCodeExecution.Engines.JintJavascript.Exceptions;
using WELearning.DynamicCodeExecution.Engines.JintJavascript.Models;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.DynamicCodeExecution.Helpers;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.DynamicCodeExecution.Engines;

public class JintJavascriptEngine : IRuntimeEngine, IDisposable
{
    private const string ExportedFunctionName = nameof(IExecutable<object>.Execute);
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _moduleCache;
    private readonly IOptions<JintOptions> _jintOptions;
    private readonly EngineCache _engineCache;
    private readonly ILockManager _lockManager;
    public JintJavascriptEngine(IOptions<JintOptions> JintOptions, ILockManager lockManager)
    {
        _lockManager = lockManager;
        var cacheOption = new MemoryCacheOptions
        {
            SizeLimit = DefaultCacheSizeLimitInBytes
        };
        _moduleCache = new MemoryCache(cacheOption);
        _engineCache = new();
        _jintOptions = JintOptions;
        // [OPT] add lib loading cache
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    private async Task<ObjectInstance> GetScriptModule(Guid engineId, Engine engine, string content, IEnumerable<string> imports, IEnumerable<Assembly> assemblies, CancellationToken cancellationToken)
    {
        var (ModuleName, CacheSize) = await GetScriptCacheEntry(engineId, content, imports, assemblies, cancellationToken);
        ObjectInstance module = null;
        _lockManager.MutexAccess(ModuleName, () =>
        {
            module = _moduleCache.GetOrCreate(ModuleName, (entry) =>
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
        });
        return module;
    }

    private static async Task<(string ModuleName, long CacheSize)> GetScriptCacheEntry(
        Guid engineId, string content, IEnumerable<string> imports,
        IEnumerable<Assembly> assemblies,
        CancellationToken cancellationToken)
    {
        using var md5 = MD5.Create();
        var importsStr = imports != null ? string.Join(string.Empty, imports) : null;
        var assembliesStr = assemblies != null ? string.Join(string.Empty, assemblies.Select(ass => ass.FullName)) : null;
        var hashContent = $"{engineId}_{content}_{importsStr}_{assembliesStr}";
        using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(hashContent));
        var hash = await md5.ComputeHashAsync(memStream, cancellationToken);
        var moduleName = Convert.ToBase64String(hash);
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '+', '=' }))
            moduleName = moduleName.Replace(c, '_');
        var contentSizeInBytes = hashContent.Length * 2;
        return (moduleName, contentSizeInBytes);
    }

    private (EngineWrap Engine, OptimizationScope Scope) PrepareEngine(Assembly[] assemblies, IEnumerable<Type> types, Guid? optimizationScopeId, CancellationToken cancellationToken)
    {
        EngineWrap CreateNewEngine()
        {
            var engine = new Engine(options: cfg =>
            {
                cfg.EnableModules(basePath: _jintOptions.Value.LibraryFolderPath, restrictToBasePath: true);
                if (assemblies?.Any() == true)
                    cfg.AllowClr(assemblies);
                cfg.DebuggerStatementHandling(Jint.Runtime.Debugger.DebuggerStatementHandling.Script);
                cfg.DebugMode(debugMode: true);
            });

            if (types?.Any() == true)
            {
                foreach (var type in types)
                    engine.SetValue(type.Name, type);
            }

            var wrap = new EngineWrap(engine);
            engine.Debugger.Skip += wrap.SetNodePosition;
            engine.Debugger.Break += wrap.SetNodePosition;
            return wrap;
        }

        if (optimizationScopeId == default) return (CreateNewEngine(), null);
        EngineWrap engine;
        if (!_engineCache.TryGetValue(optimizationScopeId.Value, out var scope))
        {
            _engineCache.RequestSlot(cancellationToken);
            try
            {
                engine = CreateNewEngine();
                scope = new OptimizationScope(engine, optimizationScopeId.Value, ReleaseSlot: _engineCache.ReleaseSlot);
                _engineCache.SetSlot(optimizationScopeId.Value, scope);
            }
            catch
            {
                _engineCache.ReleaseSlot(optimizationScopeId.Value);
                throw;
            }
        }
        else
            engine = scope.Engine;
        return (engine, scope);
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
        _moduleCache.Dispose();
        _engineCache.Dispose();
    }

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request, CancellationToken cancellationToken)
    {
        var (result, scope) = await ExecuteCore(request, cancellationToken);
        var castResult = Cast<TReturn>(result);
        return (castResult, scope);
    }

    public async Task<IDisposable> Execute<TArg>(ExecuteCodeRequest<TArg> request, CancellationToken cancellationToken)
    {
        var (_, scope) = await ExecuteCore(request, cancellationToken);
        return scope;
    }

    protected async Task<(JsValue Result, IDisposable Scope)> ExecuteCore<TArg>(ExecuteCodeRequest<TArg> request, CancellationToken cancellationToken)
    {
        var combinedAssemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types)?.ToArray();
        var (engineWrap, optimizationScope) = PrepareEngine(combinedAssemblies, request.Types, request.OptimizationScopeId, cancellationToken);
        JsValue result = default;
        var ((content, lineStart, lineEnd, indexStart, indexEnd), arguments) = PreprocessContent(request, engineWrap.Engine);

        try
        {
            await engineWrap.SafeAccessEngine(async (engine) =>
            {
                engineWrap.ResetNodePosition();
                var module = await GetScriptModule(engineId: engineWrap.Id, engine, content, request.Imports, request.Assemblies, cancellationToken);
                var exportedFunction = module.Get(ExportedFunctionName);
                result = exportedFunction.Call(arguments: arguments);
                result = result.UnwrapIfPromise();
            }, cancellationToken);
        }
        catch (ParserException ex)
        {
            throw new JintCompilationError(
                parserException: ex,
                userContentLineStart: lineStart,
                userContentLineEnd: lineEnd,
                userContentIndexStart: indexStart,
                userContentIndexEnd: indexEnd);
        }
        catch (PromiseRejectedException ex)
        {
            throw new JintRuntimeException(ex,
                content: content,
                userContentLineStart: lineStart,
                userContentLineEnd: lineEnd,
                userContentIndexStart: indexStart,
                userContentIndexEnd: indexEnd);
        }
        catch (Exception ex)
        {
            if (engineWrap.CurrentNodePosition.HasValue)
                throw new JintRuntimeException(
                    systemException: ex,
                    currentNodePosition: engineWrap.CurrentNodePosition.Value,
                    currentNodeIndex: engineWrap.CurrentNodeIndex,
                    userContentLineStart: lineStart,
                    userContentLineEnd: lineEnd,
                    userContentIndexStart: indexStart,
                    userContentIndexEnd: indexEnd);
            else throw;
        }

        return (result, optimizationScope);
    }

    private static (IEnumerable<string> Names, JsValue[] Values) GetCombineArguments<TArg>(Engine engine, TArg arguments, List<(string Name, object Value)> flattenArguments, List<string> flattenOutputs)
    {
        var argumentValues = new List<JsValue>();
        var finalArguments = new List<string>();
        flattenArguments ??= new();
        flattenOutputs ??= new();
        for (int i = 0; i < flattenArguments.Count; i++)
        {
            finalArguments.Add(flattenArguments[i].Name);
            flattenOutputs.Remove(flattenArguments[i].Name);
            argumentValues.Add(JsValue.FromObject(engine, flattenArguments[i].Value));
        }
        for (int i = 0; i < flattenOutputs.Count; i++)
            finalArguments.Add(flattenOutputs[i]);
        if (argumentValues.Count == 0)
            argumentValues.Add(JsValue.FromObjectWithType(engine, arguments, typeof(TArg)));
        return (finalArguments, argumentValues.ToArray());
    }

    private static ((string Content, int LineStart, int LineEnd, int IndexStart, int IndexEnd) ContentInfo, JsValue[] Values)
        PreprocessContent<TArg>(ExecuteCodeRequest<TArg> request, Engine engine)
    {
        const string OutVariable = "__APP_OUT__";
        var flattenOutputs = request.FlattenOutputs?.ToList();
        var (inputArguments, values) = GetCombineArguments(
            engine: engine, arguments: request.Arguments,
            flattenArguments: request.FlattenArguments?.ToList(),
            flattenOutputs: flattenOutputs);
        var flattenOutputsStr = flattenOutputs?.Any() == true ?
@$"const {OutVariable} = {{{string.Join(',', flattenOutputs)}}};
Object.keys({OutVariable}).forEach(key => {{
    if ({OutVariable}[key] === undefined) {{
        delete {OutVariable}[key];
    }}
}});
return {OutVariable};" : string.Empty;
        var contentLineCount = request.Content.NewLineCount();
        var contentInfo = request.UseRawContent
            ? (request.Content, 1, contentLineCount, 0, request.Content.Length - 1)
            : JavascriptHelper.WrapModuleFunction(
            script: request.Content,
            returnStatements: flattenOutputsStr,
            inputVariables: inputArguments);
        return (contentInfo, values);
    }

    private static TReturn Cast<TReturn>(JsValue jsValue)
    {
        object value;
        // [OPT] cast
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

    class EngineCache : IDisposable
    {
        private const long DefaultMaxEngineCacheCount = 1_000;
        private readonly ManualResetEventSlim _engineCacheWait;
        private readonly ConcurrentDictionary<Guid, OptimizationScope> _engineCache;
        private int _slotCount;

        public EngineCache()
        {
            _slotCount = 0;
            _engineCache = new();
            _engineCacheWait = new(initialState: true);
        }

        public void RequestSlot(CancellationToken cancellationToken)
        {
            bool acquired = false;
            while (!acquired)
            {
                lock (_engineCacheWait)
                {
                    if (_engineCacheWait.IsSet)
                    {
                        if (++_slotCount == DefaultMaxEngineCacheCount)
                            _engineCacheWait.Reset();
                        acquired = true;
                    }
                }
                if (!acquired)
                    _engineCacheWait.Wait(cancellationToken);
            }
        }

        public void ReleaseSlot(Guid id)
        {
            if (_engineCache.Remove(id, out _))
            {
                lock (_engineCacheWait)
                {
                    if (--_slotCount < DefaultMaxEngineCacheCount && !_engineCacheWait.IsSet)
                        _engineCacheWait.Set();
                }
            }
        }

        public void SetSlot(Guid id, OptimizationScope scope) => _engineCache[id] = scope;

        public bool TryGetValue(Guid id, out OptimizationScope scope) => _engineCache.TryGetValue(id, out scope);

        public void Dispose()
        {
            _engineCacheWait.Dispose();
            foreach (var engine in _engineCache.Values)
                engine.Dispose();
        }
    }

    class EngineWrap : IDisposable
    {
        private readonly SemaphoreSlim _lock;
        public EngineWrap(Engine engine)
        {
            _lock = new(1);
            Engine = engine;
            Id = Guid.NewGuid();
            ResetNodePosition();
        }

        public Engine Engine { get; }
        public Guid Id { get; }
        public Position? CurrentNodePosition { get; set; }
        public int CurrentNodeIndex { get; set; }

        public void Dispose()
        {
            _lock.Dispose();
            Engine.Dispose();
        }

        public void ResetNodePosition()
        {
            CurrentNodeIndex = -1;
            CurrentNodePosition = null;
        }

        public StepMode SetNodePosition(object o, DebugInformation e)
        {
            CurrentNodeIndex = e.CurrentNode?.Range.Start ?? -1;
            CurrentNodePosition = e.CurrentNode?.Location.Start;
            return default;
        }

        public async Task SafeAccessEngine(Func<Engine, Task> func, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try { await func(Engine); }
            finally { _lock.Release(); }
        }
    }

    class OptimizationScope : IDisposable
    {
        private readonly Action<Guid> ReleaseSlot;
        public OptimizationScope(
            EngineWrap engine, Guid scopeId,
            Action<Guid> ReleaseSlot)
        {
            Engine = engine;
            Id = scopeId;
            this.ReleaseSlot = ReleaseSlot;
        }

        public Guid Id { get; }
        public EngineWrap Engine { get; }

        public void Dispose()
        {
            Engine.Dispose();
            ReleaseSlot(Id);
        }
    }
}