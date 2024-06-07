using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Esprima;
using Jint;
using Jint.Constraints;
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
using WELearning.DynamicCodeExecution.Exceptions;
using WELearning.DynamicCodeExecution.Extensions;
using WELearning.DynamicCodeExecution.Helpers;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.DynamicCodeExecution.Engines;

public class JintJavascriptEngine : IRuntimeEngine, IDisposable
{
    private const string OutVariable = "__ENGINE_OUT__";
    private const int DefaultMaxStatements = 3_000_000;
    private const int DefaultMaxLoopCount = 10_000;
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
    private readonly MemoryCache _moduleCache;
    private readonly IOptions<JintOptions> _jintOptions;
    private readonly EngineCache _engineCache;
    private readonly IInMemoryLockManager _lockManager;
    public JintJavascriptEngine(IOptions<JintOptions> JintOptions, IInMemoryLockManager lockManager)
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

    private (EngineWrap Engine, OptimizationScope Scope) PrepareEngine<TArg>(ExecuteCodeRequest<TArg> request, Assembly[] assemblies)
    {
        EngineWrap CreateNewEngine()
        {
            var engine = new Engine(options: cfg =>
            {
                cfg.EnableModules(basePath: _jintOptions.Value.LibraryFolderPath, restrictToBasePath: true);
                if (assemblies?.Any() == true)
                    cfg.AllowClr(assemblies);
                cfg.DebuggerStatementHandling(DebuggerStatementHandling.Script);
                cfg.DebugMode(debugMode: true);
                cfg.MaxStatements(DefaultMaxStatements); // [NOTE] be careful with imported libraries
            });

            if (request.Types?.Any() == true)
                foreach (var type in request.Types)
                    engine.SetValue(type.Name, type);

            if (request.Modules?.Any() == true)
                foreach (var module in request.Modules)
                    AddModule(engine, module);

            var wrap = new EngineWrap(engine, DefaultMaxLoopCount);
            engine.Debugger.Skip += wrap.SetNodePosition;
            engine.Debugger.Break += wrap.SetNodePosition;
            return wrap;
        }

        if (request.OptimizationScopeId == default) return (CreateNewEngine(), null);
        EngineWrap engine;
        if (!_engineCache.TryGetValue(request.OptimizationScopeId.Value, out var scope))
        {
            _engineCache.RequestSlot(request.Tokens.Combined);
            try
            {
                engine = CreateNewEngine();
                scope = new OptimizationScope(engine, request.OptimizationScopeId.Value, ReleaseSlot: _engineCache.ReleaseSlot);
                _engineCache.SetSlot(request.OptimizationScopeId.Value, scope);
            }
            catch
            {
                _engineCache.ReleaseSlot(request.OptimizationScopeId.Value);
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

    public async Task<(TReturn Result, IDisposable OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request)
    {
        var (result, scope) = await ExecuteCore(request);
        var castResult = Cast<TReturn>(result);
        return (castResult, scope);
    }

    public async Task<IDisposable> Execute<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var (_, scope) = await ExecuteCore(request);
        return scope;
    }

    protected async Task<(JsValue Result, IDisposable Scope)> ExecuteCore<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var combinedAssemblies = ReflectionHelper.CombineAssemblies(request.Assemblies, request.Types)?.ToArray();
        var (engineWrap, optimizationScope) = PrepareEngine(request, combinedAssemblies);
        JsValue result = default;

        void DisposeEngine()
        {
            if (optimizationScope == null)
                engineWrap.Dispose();
            else optimizationScope.Dispose();
        }

        try
        {
            var ((content, lineStart, lineEnd, indexStart, indexEnd), arguments) = PreprocessContent(request, engineWrap.Engine);
            try
            {
                await engineWrap.SafeAccessEngine(async (engine) =>
                {
                    engineWrap.ResetNodePosition();
                    SetValues(engine, request.Arguments);
                    var module = await GetScriptModule(engineId: engineWrap.Id, engine, content, request.Imports, request.Assemblies, cancellationToken: request.Tokens.Combined);
                    var exportedFunction = module.Get(JsEngineConstants.ExportedFunctionName);
                    result = await CallWithHandles(engineWrap, exportedFunction, arguments, tokens: request.Tokens);
                    result = result.UnwrapIfPromise();
                }, cancellationToken: request.Tokens.Combined);
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
                if (ex is StatementsCountOverflowException stmCountOverflow)
                {
                    var maxStatements = engineWrap.MaxStatementsConstraint.MaxStatements;
                    ex = new Exception(engineWrap.IsMaxLoopCountReached
                        ? $"Possible infinite loop detected."
                        : $"The maximum number of statements executed ({maxStatements}) have been reached.");
                }
                var originalEx = (ex as AggregateException)?.InnerException ?? ex;
                if (engineWrap.CurrentNodePosition.HasValue)
                    throw new JintRuntimeException(
                        systemException: originalEx,
                        currentNodePosition: engineWrap.CurrentNodePosition.Value,
                        currentNodeIndex: engineWrap.CurrentNodeIndex,
                        userContentLineStart: lineStart,
                        userContentLineEnd: lineEnd,
                        userContentIndexStart: indexStart,
                        userContentIndexEnd: indexEnd);
                else throw;
            }

            if (optimizationScope == null)
                DisposeEngine();

            return (result, optimizationScope);
        }
        catch
        {
            DisposeEngine();
            throw;
        }
    }

    private static void SetValues<TArg>(Engine engine, TArg arguments)
    {
        var properties = arguments.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties)
            engine.SetValue(property.Name, property.GetValue(arguments));
    }

    private static async Task<JsValue> CallWithHandles(EngineWrap engineWrap, JsValue exportedFunction, JsValue[] arguments, RunTokens tokens)
    {
        var tcs = new TaskCompletionSource<JsValue>();
        JsValue result = null;
        var currentThread = Thread.CurrentThread;

        Action HandleTokensCanceled(Func<Exception> exceptionProvider) => () =>
        {
            lock (tcs) { tcs.TrySetException(exceptionProvider()); }
            currentThread.Interrupt();
            engineWrap.MaxStatementsConstraint.MaxStatements = 0; // [NOTE] terminate CPU bound logic
        };

        try
        {
            using var timeoutReg = tokens.Timeout.Register(HandleTokensCanceled(exceptionProvider: () => new TimeoutException("Execution timed out!")));
            using var terminationReg = tokens.Termination.Register(HandleTokensCanceled(exceptionProvider: () => new TerminationException("Received termination request!")));
            result = exportedFunction.Call(arguments: arguments);
            lock (tcs) { tcs.TrySetResult(result); }
        }
        catch (Exception ex)
        {
            lock (tcs) { tcs.TrySetException(ex); }
        }

        return await tcs.Task;
    }

    private static JsValue[] GetArgumentValues(Engine engine, IDictionary<string, object> inputs, IDictionary<string, object> outputs)
    {
        var argumentValues = new JsValue[2];
        argumentValues[0] = JsValue.FromObject(engine, inputs ?? new Dictionary<string, object>());
        argumentValues[1] = JsValue.FromObject(engine, outputs ?? new Dictionary<string, object>());
        return argumentValues;
    }

    private static ((string Content, int LineStart, int LineEnd, int IndexStart, int IndexEnd) ContentInfo, JsValue[] Values)
        PreprocessContent<TArg>(ExecuteCodeRequest<TArg> request, Engine engine)
    {
        var flattenArguments = new HashSet<string>();
        if (request.Inputs != null)
            foreach (var input in request.Inputs) flattenArguments.Add(input.Key);
        if (request.Outputs != null)
            foreach (var output in request.Outputs) flattenArguments.Add(output.Key);

        var values = GetArgumentValues(engine: engine, inputs: request.Inputs, outputs: request.Outputs);
        var flattenOutputs = request.Outputs?.Keys.ToList();
        var flattenOutputsStr = GetOutputPreprocessingContent(flattenOutputs);
        var contentLineCount = request.Content.NewLineCount();
        var contentInfo = request.UseRawContent
            ? (request.Content, 1, contentLineCount, 0, request.Content.Length - 1)
            : JavascriptHelper.WrapModuleFunction(
                script: request.Content, request.Async,
                returnStatements: flattenOutputsStr,
                flattenArguments: flattenArguments);
        return (contentInfo, values);
    }

    private static void AddModule(Engine engine, ImportModule module)
    {
        if (module.Functions?.Any() != true) return;
        engine.Modules.Add(module.ModuleName, builder =>
        {
            var contentBuilder = new StringBuilder();
            foreach (var function in module.Functions)
            {
                var flattenArguments = new HashSet<string>();
                if (function.Inputs != null)
                    foreach (var input in function.Inputs) flattenArguments.Add(input);
                if (function.Outputs != null)
                    foreach (var output in function.Outputs) flattenArguments.Add(output);
                var flattenOutputs = function.Outputs?.ToList();
                var flattenOutputsStr = GetOutputPreprocessingContent(flattenOutputs);

                var content = function.UseRawContent
                    ? function.Content
                    : JavascriptHelper.WrapModuleFunction(
                        script: function.Content, async: function.Async,
                        returnStatements: flattenOutputsStr,
                        functionName: function.Signature,
                        flattenArguments: flattenArguments).Content;

                contentBuilder.AppendLine(content);
            }

            var moduleContent = contentBuilder.ToString();
            builder.AddModule(Engine.PrepareModule(moduleContent));
        });
    }

    private static string GetOutputPreprocessingContent(IEnumerable<string> flattenOutputs)
    {
        var flattenOutputsStr = flattenOutputs?.Any() == true ?
@$"const {OutVariable} = {{{string.Join(',', flattenOutputs)}}};
Object.keys({OutVariable}).forEach(key => {{
    if ({OutVariable}[key] === undefined) {{
        delete {OutVariable}[key];
    }}
}});
return {OutVariable};" : string.Empty;
        return flattenOutputsStr;
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
        private Dictionary<Esprima.Ast.Node, int> _nodesCount;
        private readonly SemaphoreSlim _lock;
        public EngineWrap(Engine engine, int maxLoopCount)
        {
            _lock = new(1);
            _nodesCount = new();
            MaxLoopCount = maxLoopCount;
            Engine = engine;
            MaxStatementsConstraint = engine.Constraints.Find<MaxStatementsConstraint>();
            Id = Guid.NewGuid();
            ResetNodePosition();
        }

        public Engine Engine { get; }
        public Guid Id { get; }
        public Position? CurrentNodePosition { get; private set; }
        public int CurrentNodeIndex { get; private set; }
        public int MaxLoopCount { get; }
        public bool IsMaxLoopCountReached { get; private set; }
        public MaxStatementsConstraint MaxStatementsConstraint { get; }

        public void Dispose()
        {
            _nodesCount = null;
            _lock.Dispose();
            Engine.Dispose();
        }

        public void ResetNodePosition()
        {
            _nodesCount = new();
            CurrentNodeIndex = -1;
            CurrentNodePosition = null;
        }

        public StepMode SetNodePosition(object o, DebugInformation e)
        {
            if (e.CurrentNode != null)
            {
                if (!_nodesCount.TryGetValue(e.CurrentNode, out int count))
                    _nodesCount[e.CurrentNode] = count;
                var nodeCount = _nodesCount[e.CurrentNode] = count + 1;
                if (nodeCount > MaxLoopCount)
                {
                    MaxStatementsConstraint.MaxStatements = MaxLoopCount;
                    IsMaxLoopCountReached = true;
                }
            }
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