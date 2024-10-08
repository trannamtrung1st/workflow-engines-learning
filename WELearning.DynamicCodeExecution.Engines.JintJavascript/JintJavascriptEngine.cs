using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
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
using TNT.Boilerplates.Concurrency.Abstracts;
using Acornima;
using Jint.Runtime.Interop;
using System.Diagnostics.CodeAnalysis;

namespace WELearning.DynamicCodeExecution.Engines;

public class JintJavascriptEngine : IRuntimeEngine, IDisposable
{
    public const string StatementSourceExternal = "external";
    public const string StatementSourceUser = "user";
    private const string ExportedFunctionName = JsEngineConstants.ExportedFunctionName;
    private const string MainFunction = JsEngineConstants.MainFunction;
    private const int DefaultMaxStatements = 3_000_000;
    private const int DefaultMaxLoopCount = 10_000;
    private const long DefaultCacheSizeLimitInBytes = 30_000_000;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);
    private readonly MemoryCache _preparedContentCache;
    private readonly MemoryCache _preprocessedContentCache;
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
        _preparedContentCache = new(cacheOption);
        _preprocessedContentCache = new(cacheOption);
        _engineCache = new();
        _jintOptions = JintOptions;
    }

    public bool CanRun(ERuntime runtime) => runtime == ERuntime.Javascript;

    private static (string ModuleName, long CacheSize) GetModuleObjectCacheEntry(Guid engineId, string contentId, string content)
    {
        var moduleName = $"{engineId}_{contentId}";
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '+', '=' }))
            moduleName = moduleName.Replace(c, '_');
        var contentSizeInBytes = content.Length * 2;
        return (moduleName, contentSizeInBytes);
    }

    private (EngineWrap Engine, OptimizationScope Scope) PrepareEngine(CompileCodeRequest request, Assembly[] assemblies)
    {
        EngineWrap CreateNewEngine()
        {
            var engine = new Engine(options: cfg =>
            {
                if (!string.IsNullOrEmpty(_jintOptions.Value.LibraryFolderPath))
                    cfg.EnableModules(basePath: _jintOptions.Value.LibraryFolderPath, restrictToBasePath: true);
                if (assemblies?.Any() == true)
                    cfg.AllowClr(assemblies);
                if (request.Extensions?.Length > 0)
                    cfg.AddExtensionMethods(request.Extensions);
                cfg.DebuggerStatementHandling(DebuggerStatementHandling.Script);
                cfg.DebugMode(debugMode: true);
                cfg.MaxStatements(DefaultMaxStatements); // [NOTE] be careful with imported libraries
                cfg.AddObjectConverter<ExceptionObjectConverter>();
                cfg.ExperimentalFeatures = ExperimentalFeature.TaskInterop;
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

        if (request.OptimizationScopeId == default)
            return (CreateNewEngine(), null);

        EngineWrap engine = null; OptimizationScope scope = null;
        var scopeId = request.OptimizationScopeId;
        void CreateScope()
        {
            _engineCache.RequestSlot(request.Tokens.Combined);
            try
            {
                engine = CreateNewEngine();
                scope = new OptimizationScope(engine, scopeId, ReleaseSlot: _engineCache.ReleaseSlot);
                scope.Acquire();
                _engineCache.SetSlot(scopeId, scope);
            }
            catch
            {
                _engineCache.ReleaseSlot(scopeId);
                throw;
            }
        }

        _lockManager.MutexAccess(key: $"ENGINES.{scopeId}", () =>
        {
            if (!_engineCache.TryGetValue(scopeId, out scope))
                CreateScope();
            else if (!scope.Acquire())
                CreateScope();
            engine = scope.Engine;
        });
        return (engine, scope);
    }

    private static string GetImportStatements(IEnumerable<string> imports)
    {
        var nl = Environment.NewLine;
        var importPart = string.Empty;
        if (imports?.Any() == true)
            importPart = string.Join(nl, imports) + nl;
        return importPart;
    }

    public void Dispose()
    {
        _preprocessedContentCache.Dispose();
        _preparedContentCache.Dispose();
        _engineCache.Dispose();
    }

    public async Task<(TReturn Result, IOptimizationScope OptimizationScope)> Execute<TReturn, TArg>(ExecuteCodeRequest<TArg> request)
    {
        var (result, scope) = await ExecuteCore(compileRequest: request, executeRequest: request);
        var castResult = Cast<TReturn>(result);
        return (castResult, scope);
    }

    public async Task<IOptimizationScope> Execute<TArg>(ExecuteCodeRequest<TArg> request)
    {
        var (_, scope) = await ExecuteCore(compileRequest: request, executeRequest: request);
        return scope;
    }

    protected async Task<(JsValue Result, IOptimizationScope Scope)> ExecuteCore<TArg>(CompileCodeRequest compileRequest, ExecuteCodeRequest<TArg> executeRequest)
    {
        var combinedAssemblies = ReflectionHelper.CombineAssemblies(compileRequest.Assemblies, compileRequest.Types)?.ToArray();
        var (engineWrap, optimizationScope) = PrepareEngine(compileRequest, combinedAssemblies);
        JsValue result = default;

        void DisposeEngine()
        {
            if (optimizationScope == null)
                engineWrap.Dispose();
            else optimizationScope.Dispose();
        }

        try
        {
            var userContentInfo = PreprocessContent(compileRequest);
            try
            {
                if (executeRequest != null)
                {
                    var arguments = GetArgumentValues(engine: engineWrap.Engine, inputs: executeRequest.Inputs, outputs: executeRequest.Outputs);
                    result = compileRequest.IsScriptOnly
                        ? await ExecuteScript(executeRequest, engineWrap, userContentInfo)
                        : await ExecuteModule(executeRequest, engineWrap, arguments, userContentInfo);
                }
                else
                {
                    if (compileRequest.IsScriptOnly)
                        CompileScript(compileRequest, content: userContentInfo.Content);
                    else await CompileModule(compileRequest, engineWrap, content: userContentInfo.Content);
                }
            }
            catch (ParseErrorException ex)
            {
                throw new JintCompilationError(parserException: ex, contentInfo: userContentInfo);
            }
            catch (ScriptPreparationException ex) when (ex.InnerException is ParseErrorException parseEx)
            {
                throw new JintCompilationError(parserException: parseEx, contentInfo: userContentInfo);
            }
            catch (PromiseRejectedException ex)
            {
                throw new JintRuntimeException(
                    ex, mainFunction: MainFunction,
                    currentNodeLocation: engineWrap.CurrentNodeLocation,
                    contentInfo: userContentInfo);
            }
            catch (JavaScriptException ex)
            {
                throw new JintRuntimeException(
                    ex, mainFunction: MainFunction,
                    currentNodeLocation: engineWrap.CurrentNodeLocation,
                    contentInfo: userContentInfo);
            }
            catch (Exception ex)
            {
                ex = (ex as AggregateException)?.InnerException ?? ex;
                var isUserSource = false;

                if (ex is StatementsCountOverflowException stmCountOverflow)
                {
                    var maxStatements = engineWrap.MaxStatementsConstraint.MaxStatements;
                    ex = new Exception(engineWrap.IsMaxLoopCountReached
                        ? $"Possible infinite loop detected."
                        : $"The maximum number of statements executed ({maxStatements}) have been reached.");
                    isUserSource = true;
                }

                if (engineWrap.CurrentNodePosition.HasValue)
                    throw new JintRuntimeException(
                        systemException: ex, isUserSource,
                        currentNodePosition: engineWrap.CurrentNodePosition.Value,
                        currentNodeLocation: engineWrap.CurrentNodeLocation,
                        contentInfo: userContentInfo);

                throw;
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
        finally
        {
            optimizationScope?.Release();
        }
    }

    private async Task<JsValue> ExecuteModule<TArg>(ExecuteCodeRequest<TArg> request, EngineWrap engineWrap, JsValue[] arguments, UserContentInfo contentInfo)
    {
        JsValue result = default;
        var preparedModule = GetPreparedModule(key: request.ContentId, () => contentInfo.Content);
        var (moduleName, cacheSize) = GetModuleObjectCacheEntry(engineId: engineWrap.Id, contentId: request.ContentId, contentInfo.Content);
        await engineWrap.SafeAccessEngine(async (engine) =>
        {
            engineWrap.ResetNodePosition();
            SetValues(engine, request.Arguments);
            var module = _lockManager.MutexAccess($"MODULE_OBJECTS.{moduleName}",
                task: () => engineWrap.GetModuleObject(preparedModule, moduleName, cacheSize));
            var exportedFunction = module.Get(ExportedFunctionName);
            result = await CallWithHandles(engineWrap, callFunction: () => exportedFunction.Call(arguments), tokens: request.Tokens);
            result = result.UnwrapIfPromise();
        }, request, contentInfo, cancellationToken: request.Tokens.Combined);
        return result;
    }

    private async Task<JsValue> ExecuteScript<TArg>(ExecuteCodeRequest<TArg> request, EngineWrap engineWrap, UserContentInfo contentInfo)
    {
        JsValue result = default;
        var preparedScript = GetPreparedScript(key: request.ContentId, () => contentInfo.Content);
        await engineWrap.SafeAccessEngine(async (engine) =>
        {
            engineWrap.ResetNodePosition();
            SetValues(engine, request.Arguments);
            result = await CallWithHandles(engineWrap, callFunction: () => engine.Evaluate(preparedScript), tokens: request.Tokens);
            result = result.UnwrapIfPromise();
        }, request, contentInfo, cancellationToken: request.Tokens.Combined);
        return result;
    }

    private async Task CompileModule(CompileCodeRequest request, EngineWrap engineWrap, string content)
    {
        var preparedModule = GetPreparedModule(key: request.ContentId, () => content);
        var (moduleName, cacheSize) = GetModuleObjectCacheEntry(engineId: engineWrap.Id, contentId: request.ContentId, content);
        await engineWrap.SafeAccessEngine((engine) =>
        {
            engineWrap.ResetNodePosition();
            var module = _lockManager.MutexAccess($"MODULE_OBJECTS.{moduleName}",
                task: () => engineWrap.GetModuleObject(preparedModule, moduleName, cacheSize));
            var exportedFunction = module.Get(ExportedFunctionName);
            return Task.CompletedTask;
        }, request: null, contentInfo: null, cancellationToken: request.Tokens.Combined);
    }

    private void CompileScript(CompileCodeRequest request, string content) => GetPreparedScript(key: request.ContentId, () => content);

    private static void SetValues<TArg>(Engine engine, TArg arguments)
    {
        if (arguments is IArguments argumentsObj)
        {
            foreach (var kvp in argumentsObj.GetArguments())
                engine.SetValue(kvp.Key, kvp.Value);
        }
        else if (arguments is IEnumerable<KeyValuePair<string, object>> dict)
        {
            foreach (var kvp in dict)
                engine.SetValue(kvp.Key, kvp.Value);
        }
        else
        {
            var properties = arguments.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
                engine.SetValue(property.Name, property.GetValue(arguments));
        }
    }

    private static async Task<JsValue> CallWithHandles(EngineWrap engineWrap, Func<JsValue> callFunction, RunTokens tokens)
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
            using var _1 = tokens.Timeout.Register(HandleTokensCanceled(exceptionProvider: () => new TimeoutException("Execution timed out!")));
            using var _2 = tokens.Termination.Register(HandleTokensCanceled(exceptionProvider: () => new TerminatedException("Received termination request!")));
            using var _3 = tokens.Exception.Register(HandleTokensCanceled(exceptionProvider: () => new TerminatedException("Terminated due to exception!")));
            result = callFunction();
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

    private UserContentInfo PreprocessContent(CompileCodeRequest compileRequest)
    {
        return _lockManager.MutexAccess(key: $"CONTENTS.{compileRequest.ContentId}",
            task: () => _preprocessedContentCache.GetOrCreate(compileRequest.ContentId, (entry) =>
            {
                UserContentInfo contentInfo;
                var lines = compileRequest.Content.BreakLines();
                if (compileRequest.UseRawContent || compileRequest.IsScriptOnly)
                {
                    contentInfo = (compileRequest.Content, lines, 1, lines.Length, 0, compileRequest.Content.Length);
                }
                else
                {
                    var flattenArguments = new HashSet<string>();
                    if (compileRequest.Inputs != null)
                        foreach (var input in compileRequest.Inputs)
                            flattenArguments.Add(input);
                    if (compileRequest.Outputs != null)
                        foreach (var output in compileRequest.Outputs)
                            flattenArguments.Add(output);

                    var flattenOutputs = compileRequest.Outputs?.ToList();
                    var importStatements = GetImportStatements(compileRequest.Imports);

                    contentInfo = JavascriptHelper.WrapModuleFunction(
                            script: compileRequest.Content, async: compileRequest.Async, isScript: compileRequest.IsScriptOnly,
                            topStatements: importStatements,
                            flattenArguments: flattenArguments,
                            flattenOutputs: flattenOutputs);
                }

                var contentSizeInBytes = contentInfo.Content.Length * 2;
                entry.SetSize(contentSizeInBytes);
                entry.SetSlidingExpiration(DefaultSlidingExpiration);
                return contentInfo;
            }));
    }

    private void AddModule(Engine engine, ImportModule module)
    {
        if (module.Functions?.Any() != true) return;

        var preparedModule = GetPreparedModule($"MODULES.{module.Id}", () =>
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

                var content = function.UseRawContent
                    ? function.Content
                    : JavascriptHelper.WrapModuleFunction(
                        script: function.Content, async: function.Async,
                        functionName: function.Signature,
                        flattenArguments: flattenArguments,
                        flattenOutputs: flattenOutputs).Content;

                contentBuilder.AppendLine(content);
            }

            var moduleContent = contentBuilder.ToString();
            return moduleContent;
        });

        engine.Modules.Add(module.ModuleName, b => b.AddModule(preparedModule));
    }

    private Prepared<Acornima.Ast.Module> GetPreparedModule(string key, Func<string> contentProvider)
    {
        return _lockManager.MutexAccess($"PREPARED_CONTENTS.{key}",
            task: () => _preparedContentCache.GetOrCreate(key, (entry) =>
            {
                var content = contentProvider();
                var contentSizeInBytes = content.Length * 2;
                entry.SetSize(contentSizeInBytes);
                entry.SetSlidingExpiration(DefaultSlidingExpiration);
                return Engine.PrepareModule(content);
            }));
    }

    private Prepared<Acornima.Ast.Script> GetPreparedScript(string key, Func<string> contentProvider)
    {
        return _lockManager.MutexAccess($"PREPARED_CONTENTS.{key}",
            task: () => _preparedContentCache.GetOrCreate(key, (entry) =>
            {
                var content = contentProvider();
                var contentSizeInBytes = content.Length * 2;
                entry.SetSize(contentSizeInBytes);
                entry.SetSlidingExpiration(DefaultSlidingExpiration);
                return Engine.PrepareScript(content);
            }));
    }

    private static TReturn Cast<TReturn>(JsValue jsValue)
    {
        object value;
        switch (jsValue.Type)
        {
            case Jint.Runtime.Types.Boolean: value = jsValue.AsBoolean(); break;
            case Jint.Runtime.Types.Number: value = jsValue.AsNumber(); break;
            case Jint.Runtime.Types.Object:
                {
                    if (jsValue.IsDate())
                    {
                        var dValue = jsValue.AsDate();
                        value = double.IsNaN(dValue.DateValue) ? dValue.DateValue : dValue.ToDateTime();
                    }
                    else
                        value = jsValue.ToObject();
                    break;
                }
            case Jint.Runtime.Types.String: value = jsValue.AsString(); break;
            case Jint.Runtime.Types.Empty:
            case Jint.Runtime.Types.Undefined:
            case Jint.Runtime.Types.Null: return default;
            default: throw new NotSupportedException($"{jsValue.Type}");
        }
        return (TReturn)value;
    }

    public async Task<IOptimizationScope> Compile(CompileCodeRequest request)
    {
        var (_, scope) = await ExecuteCore<object>(compileRequest: request, executeRequest: null);
        return scope;
    }

    class EngineCache : IDisposable
    {
        private const long DefaultMaxEngineCacheCount = 3_000;
        private readonly ManualResetEventSlim _engineCacheWait;
        private readonly ConcurrentDictionary<string, OptimizationScope> _scopeCache;
        private int _slotCount;

        public EngineCache()
        {
            _slotCount = 0;
            _scopeCache = new();
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

        public void ReleaseSlot(string id)
        {
            if (_scopeCache.Remove(id, out _))
            {
                lock (_engineCacheWait)
                {
                    if (--_slotCount < DefaultMaxEngineCacheCount && !_engineCacheWait.IsSet)
                        _engineCacheWait.Set();
                }
            }
        }

        public void SetSlot(string id, OptimizationScope scope) => _scopeCache[id] = scope;

        public bool TryGetValue(string id, out OptimizationScope scope) => _scopeCache.TryGetValue(id, out scope);

        public void Dispose()
        {
            _engineCacheWait.Dispose();
            foreach (var scope in _scopeCache.Values)
                scope.Dispose();
        }
    }

    class EngineWrap : IDisposable
    {
        private readonly Dictionary<string, ObjectInstance> _moduleCache;
        private Dictionary<Acornima.Ast.Node, int> _nodesCount;
        private readonly SemaphoreSlim _lock;
        private int? _mainStackCount;

        public EngineWrap(Engine engine, int maxLoopCount)
        {
            _moduleCache = [];
            _lock = new(1, 1);
            _nodesCount = [];
            MaxLoopCount = maxLoopCount;
            Engine = engine;
            MaxStatementsConstraint = engine.Constraints.Find<MaxStatementsConstraint>();
            Id = Guid.NewGuid();
            ResetNodePosition();
        }

        public Engine Engine { get; }
        public Guid Id { get; }
        public ExecuteCodeRequest CurrentExecuteRequest { get; private set; }
        public UserContentInfo? CurrentContentInfo { get; private set; }
        public Position? CurrentNodePosition { get; private set; }
        public (int Start, int End) CurrentNodeLocation { get; private set; }
        public int MaxLoopCount { get; }
        public bool IsMaxLoopCountReached { get; private set; }
        public MaxStatementsConstraint MaxStatementsConstraint { get; }

        public void Dispose()
        {
            _nodesCount = null;
            _moduleCache.Clear();
            _lock.Dispose();
            Engine.Dispose();
        }

        public void ResetNodePosition()
        {
            _nodesCount = new();
            CurrentNodeLocation = (-1, -1);
            CurrentNodePosition = null;
        }

        public StepMode SetNodePosition(object o, DebugInformation e)
        {
            if (e.CurrentNode != null)
            {
                var range = e.CurrentNode.Range;

                if (e.CallStack.Count > 0 && (_mainStackCount == null || e.CallStack.Count == _mainStackCount) && e.CallStack[0].FunctionName == MainFunction)
                {
                    _mainStackCount = e.CallStack.Count;
                    CurrentNodeLocation = (range.Start, range.End);
                    CurrentNodePosition = e.CurrentNode.Location.Start;
                }

                if (CurrentExecuteRequest?.Tracker != null)
                {
                    var position = e.CurrentNode.Location.Start;
                    var (Line, LineEnd, Column, ColumnEnd, StartIndex, EndIndex) = DiagnosticHelper.RecalculatePosition(
                        lineNumber: position.Line,
                        column: position.Column,
                        location: (range.Start, range.End),
                        contentInfo: CurrentContentInfo.Value);
                    CurrentExecuteRequest.Tracker.CurrentStatement = new CodeStatement
                    {
                        LineNumber = Line,
                        LineNumberEnd = LineEnd,
                        Column = Column,
                        ColumnEnd = ColumnEnd,
                        StartIndex = StartIndex,
                        EndIndex = EndIndex,
                        FunctionName = e.CurrentCallFrame.FunctionName,
                        Source = Line == -1 ? StatementSourceExternal : StatementSourceUser
                    };
                }

                if (!_nodesCount.TryGetValue(e.CurrentNode, out int count))
                    _nodesCount[e.CurrentNode] = count;

                var nodeCount = _nodesCount[e.CurrentNode] = count + 1;
                if (nodeCount > MaxLoopCount)
                {
                    MaxStatementsConstraint.MaxStatements = MaxLoopCount;
                    IsMaxLoopCountReached = true;
                }
            }
            return default;
        }

        public async Task SafeAccessEngine(Func<Engine, Task> task, ExecuteCodeRequest request, UserContentInfo? contentInfo, CancellationToken cancellationToken)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                CurrentExecuteRequest = request;
                CurrentContentInfo = contentInfo;
                await task(Engine);
            }
            finally { _lock.Release(); }
        }

        public ObjectInstance GetModuleObject(Prepared<Acornima.Ast.Module> preparedModule, string moduleName, long cacheSize)
        {
            if (!_moduleCache.TryGetValue(moduleName, out var module))
            {
                Engine.Modules.Add(moduleName, b => b.AddModule(preparedModule));
                module = Engine.Modules.Import(moduleName);
                _moduleCache[moduleName] = module;
            }
            return module;
        }
    }

    class OptimizationScope : IOptimizationScope
    {
        private readonly Action<string> ReleaseSlot;
        private int _acquiredCount = 0;
        private bool _disposed = false;
        public OptimizationScope(
            EngineWrap engine, string scopeId,
            Action<string> ReleaseSlot)
        {
            Engine = engine;
            Id = scopeId;
            this.ReleaseSlot = ReleaseSlot;
        }

        public string Id { get; }
        public EngineWrap Engine { get; }

        public bool Acquire()
        {
            lock (this)
            {
                if (_disposed)
                    return false;
                _acquiredCount++;
                return true;
            }
        }

        public void Release()
        {
            lock (this) { _acquiredCount--; }
        }

        public void Dispose()
        {
            _disposed = true;
            Engine.Dispose();
            ReleaseSlot(Id);
        }

        public bool TryDispose()
        {
            lock (this)
            {
                if (_acquiredCount <= 0)
                {
                    Dispose();
                    return true;
                }
            }
            return false;
        }
    }

    class ExceptionObjectConverter : IObjectConverter
    {
        public bool TryConvert(Engine engine, object value, [NotNullWhen(true)] out JsValue result)
        {
            result = null;
            if (value is not Exception ex)
                return false;

            result = JsValue.FromObject(engine, new Dictionary<string, object>
            {
                ["message"] = ex.InnerException?.Message ?? ex.Message
            });
            return true;
        }
    }
}
